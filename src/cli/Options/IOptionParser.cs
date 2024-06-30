using System;

namespace cli.Options;

public interface IOptionParser//<T>
{
    object Parse(string option);
}