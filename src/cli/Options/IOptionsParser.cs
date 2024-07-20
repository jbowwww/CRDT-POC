namespace cli.Options;

public interface IOptionsParser<T>
{
    T Parse(string option);
}