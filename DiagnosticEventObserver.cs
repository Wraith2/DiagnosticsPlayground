namespace DiagnosticsPlayground
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;

    public class DiagnosticEventObserver : IObserver<KeyValuePair<string, object?>>
    {
        private bool completed;
        private DiagnosticListener listener;
        //[ThreadStatic] private StringBuilder? buffer;

        public DiagnosticEventObserver(DiagnosticListener value)
        {
            listener = value;
        }

        public bool IsCompleted => completed;

        public DiagnosticListener Listener => listener;

        public void OnCompleted()
        {
            completed = true;
        }

        public void OnError(Exception error)
        {

        }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (!completed)
            {
   
                StringBuilder buffer = new StringBuilder(1024);
                var depth = 0;
                var activity = Activity.Current;
                if (activity != null)
                {
                    var activityId = activity.Id;
                    buffer.Append(activity.Id);
                    buffer.Append(' ', Math.Max(1, 35 - activityId.Length));
                    //buffer.Append('\t');
                    var parent = activity.Parent;
                    while (parent != null)
                    {
                        depth += 1;
                        parent = parent.Parent;
                    }
                }


                buffer.Append(' ', depth * 2);

                //buffer.Append(listener.Name);
                //buffer.Append('.');
                buffer.Append(value.Key);
                buffer.Append(' ', Math.Max(1,60 - buffer.Length));
                buffer.Append(' ');



                var baggage = Activity.Current?.Baggage;
                int baggageCount = 0;
                if (baggage != null)
                {
                    foreach (var pair in baggage)
                    {
                        if (baggageCount > 0)
                        {
                            buffer.Append(", ");
                        }
                        else
                        {
                            buffer.Append("[ ");
                        }
                        buffer.Append(pair.Key);
                        buffer.Append('=');
                        buffer.Append(pair.Value);
                        baggageCount += 1;
                    }
                    if (baggageCount > 0)
                    {
                        buffer.Append(" ]");
                    }
                }

                if (value.Value != null)
                {
                    if (baggageCount > 0)
                    {
                        buffer.Append(' ');
                    }
                    buffer.Append("{ ");
                    if (value.Value is IEnumerable<KeyValuePair<string, object?>> enumerable)
                    {
                        int valueCount = 0;
                        foreach (var pair in enumerable)
                        {
                            if (valueCount > 0)
                            {
                                buffer.Append(", ");
                            }
                            buffer.Append(pair.Key);
                            buffer.Append('=');
                            buffer.Append(pair.Value?.ToString() ?? string.Empty);
                            valueCount += 1;
                        }
                    }
                    else
                    {
                        buffer.Append(value.Value?.ToString());
                    }
                    buffer.Append(" }");
                }

                Console.WriteLine(buffer.ToString());
                buffer.Clear();
            }
        }
    }

}
