namespace OpenTkWPFHost.Core
{
    public readonly struct PropertyChangedArgs<T>
    {
        public T NewValue { get; }
        public T OldValue { get; }

        public PropertyChangedArgs(T oldValue, T newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}