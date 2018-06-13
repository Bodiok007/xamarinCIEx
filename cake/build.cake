var target = Argument("target", "Default");

Task("Default")
  .Does(() =>
{
  Information("Hello World!" + target);
  Information(target);
});

RunTarget(target);