# Local Software Factory

These are the most recent versions of my experimental scripting for creating a local software factory. There are two folders in this repository, one for my work in Python using the Cursor/Grok Build SDK, and another for my work in C# using the Copilot SDK.

I'll reference this repo in [my blog post for my "Command Your Droids" presentation](https://jmarkman.github.io/blog/command-your-droids-techbash-2026/) I gave at TechBash 2026.

For the Python version, I relied on Cursor's /sdk command to find my way through the dark in terms of how their SDK worked. The documentation is good but not great, and figuring out how what their message DTOs were and what information they contained was a real pain.

For the C# version, the Copilot SDK has more robust documentation and examples. I wrote the examples and the main software factory as [file-based apps](https://devblogs.microsoft.com/dotnet/announcing-dotnet-run-app/).

`getting-started` and `streaming` are copied straight from their docs, and `process` was me figuring out how to port the Python version's usage of `subprocess.run()` to check the backlog list.

Feel free to use these as a springboard for your own scripts. I'll update this repo sometimes as I experiment more, but be aware that all of this AI stuff is moving pretty fast and I will inevitably move on alongside it.