# Rendrix Engine
A Pure C# Ascii Based Game Engine
---
## About the Engine
- The Engine is purly made with C#, no c++ bindings
- The Engine uses basic ASCII Characters to Draw the 3D Objects on the Screen
---
## Getting Started
1. Clone the Repository
    - **The Engine uses .net 8 so you need to install it to your system**
2. Run `dotnet build` to build all the Examples and the Engine Library
    - **To only compile one of the example use `dotnet run --project "example project path"`**
---
## How to Make Games using the Engine
1. Add the `RendrixEngine` Project as Project Reference to your Game Project or add the pre-compiled library as Asembely Reference
2. To Run the Engine Import the namespace and Initialize the Engine like this:
```csharp
    using RendrixEngine;

    public class Program
    {
        public static void Main(string[] args)
        {
            Engine engine = new Engine(
                width: 120,
                height: 40,
                targetFPS: 30,
                title: "My Very Cool Game",
                ambientStrength: 0.3f
            );

            engine.Initialize();
        }
    }
```
* *Anything beyond the Main Initialize method wont run since it Blocks the main Thread to prevent a instant Exit*
---
## TODO's
- [x] Implement ASCII Renderer
- [x] Implement Lighting
- [x] Implement Entity Component Engine
- [] Implement Color and Monochrome Mode
- [] Make Engine Use a Custom Avalonia Window for Character Displayment
- [] Make Camera be a seperate Component with multi Camera Instance Support