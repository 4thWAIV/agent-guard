public interface CertifiedResults<T>
{
    public T Results { get; }
    public string Hash { get; }
    public string Signature { get; }
}
