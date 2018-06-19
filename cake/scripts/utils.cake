String Combine(params String[] parts)
{
    return System.IO.Path.Combine(parts);
}

String EnvVariable(string key, bool throwIfEmpty = true)
{
    var variable = EnvironmentVariable(key);

    if (string.IsNullOrEmpty(variable) && throwIfEmpty)
    {
        throw new Exception($"The {key} environment variable is not defined.");
    }

    return variable;
}