using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DiagnosticsPlayground
{
    class Program
    {
        private static readonly DiagnosticSource diagnostics = new DiagnosticListener(typeof(Program).FullName);
        private static int ReceiveCounter;
        private static int EventCounter;

        static void Main(string[] args)
        {
            DiagnosticListenerObserver observer = new DiagnosticListenerObserver();
            IDisposable? observed = DiagnosticListener.AllListeners.Subscribe(observer);
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            Console.WriteLine("Press any key to stop");

            var activity = new Activity(nameof(Main));
            diagnostics.StartActivity(activity, null);

            Task.Run(() => ReceiveEvent(cancellationTokenSource.Token));

            Console.ReadKey(true);

            cancellationTokenSource.Cancel();

            diagnostics.StopActivity(activity, null);

            observer.OnCompleted();
        }

        public async static Task ReceiveEvent(CancellationToken cancellationToken)
        {
            var outerActivity = new Activity("SimulateLoop");
            diagnostics.StartActivity(outerActivity, null);

            Random random = new Random();

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(random.Next(100, 1000));

                    int receiptId = Interlocked.Increment(ref ReceiveCounter);
                    var activity = new Activity(nameof(ReceiveEvent));
                    activity.AddBaggage("rid", receiptId.ToString());
                    diagnostics.StartActivity(activity, null);
                    if ((random.Next(1, 100) % 2) == 0)
                    {
                        diagnostics.Write(nameof(ReceiveEvent) + ".Take", new { rid = receiptId });
                        ProcessEvent(receiptId, cancellationToken);
                    }
                    else
                    {
                        diagnostics.Write(nameof(ReceiveEvent) + ".Drop", new { rid = receiptId });
                    }

                    diagnostics.StopActivity(activity, null);
                }
            }
            finally
            {
                diagnostics.StopActivity(outerActivity, null);
            }
        }

        public static void ProcessEvent(int receiveID, CancellationToken cancellationToken)
        {
            // this method tries to logically disconnect the incoming event from the further processing.
            // the event has been successfully received and now we want to initate an internal process to 
            // use the data

            if (!cancellationToken.IsCancellationRequested)
            {
                int eventId = Interlocked.Increment(ref EventCounter);
                var activity = new Activity(nameof(ProcessEvent));
                activity.AddBaggage("eid", eventId.ToString());  // gives the eid+rid correlation
                diagnostics.StartActivity(activity, null);

                // suppressing the execution context prevents the async local for the activity
                // from flowing from the current thread/task to the new thread/task but it seems
                // a bit overkill because it'll stop all over async locals as well, like impersonation
                //using (ExecutionContext.SuppressFlow())
                {
                    _ = Task.Factory.StartNew(
                        function: InternalProcess,
                        state: eventId,
                        cancellationToken: cancellationToken,
                        TaskCreationOptions.DenyChildAttach,
                        TaskScheduler.Default
                    );
                }

                diagnostics.StopActivity(activity, null);
            }
        }

        public async static Task InternalProcess(object state)
        {
            int eventId = (int)state;
            var activity = new Activity(nameof(InternalProcess));
            // add the eid because we're disconneted from the parent so have no baggage
            activity.AddBaggage("eid", eventId.ToString());
            diagnostics.StartActivity(activity, null);

            await Task.Delay(123);

            diagnostics.Write(nameof(InternalProcess) + ".Work", new { result = eventId * 2 });

            diagnostics.StopActivity(activity, null);
        }
    }
}
