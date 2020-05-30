namespace AsyncEnumerable.Collections.Models
{
    internal class AsyncEnumerableTaskResult<T>
    {
        public bool Emit { get; set; }
        public bool Stop { get; set; }
        public T Result { get; set; }
    }
}
