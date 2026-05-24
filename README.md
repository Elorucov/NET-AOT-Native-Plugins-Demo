__A sample .NET app with native plugin.__

Demonstration of calling a function from a native library, written in C, from the host side, written in .NET NativeAOT.

### Building host

```
dotnet publish -r win-x64 -c Release
```

### Building plugin

For Windows: open VS developer command prompt, `cd` to folder with .c file, then...

```
cmake -S . -B build
cmake --build build
```

For Linux, just call `make`. (not yet tested)
