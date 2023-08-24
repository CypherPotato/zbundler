namespace zbundler.src;

public interface IBuilder
{
    public string Name { get; }
    public BuildMode BuildMode { get; set; }
    public void Build(Configuration configuration);
}

public enum BuildMode
{
    OneToOne,
    ManyToOne
}
