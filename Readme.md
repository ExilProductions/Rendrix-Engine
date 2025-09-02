# Rendrix Engine

A pure C# ASCII-based game engine.

![.NET 8](https://img.shields.io/badge/.NET-8-blue.svg)
![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)
![Status: In Development](https://img.shields.io/badge/Status-In%20Development-yellow.svg)

---

## About

* Written entirely in C# (no C++ bindings).
* Renders 3D objects directly in the console using ASCII characters.

---

## Getting Started

1. Clone the repository.
   **Note:** The engine requires **.NET 8**. Make sure it is installed on your system.

2. Build the engine and examples:

   ```bash
   dotnet build
   ```

3. Run a specific example:

   ```bash
   dotnet run --project "path-to-example"
   ```

---

## Creating Games with Rendrix

1. Add the `RendrixEngine` project as a project reference, or include the precompiled library as an assembly reference.
2. Import the namespace and initialize the engine:

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

### Notes

* The `Initialize` method blocks the main thread, preventing the program from exiting immediately:

  ```csharp
  engine.Initialize();
  Console.WriteLine("Thank you for playing my Game"); 
  // This will only run after engine.Stop() is called
  ```

* `RootNodes` are equivalent to Unity’s `GameObject`s.
  Since Rendrix is script-only, use the built-in `RootNode` to attach child `SceneNode`s.
  Components can be added to both `RootNode` and child nodes.

  ```csharp
  var myNode = new SceneNode("MyObject");
  engine.RootNode.AddChild(myNode);
  ```

---

## Roadmap

* [x] ASCII renderer
* [x] Lighting system
* [x] Entity-component system
* [ ] Color and monochrome rendering modes
* [x] Custom Avalonia window for character rendering
* [x] Camera as a separate component with multi-camera support
* [ ] Configurable renderer at runtime
* [ ] Audio Engine using NAudio
+ [ ] Physics Engine using a Custom Physics Engine or a third party one

---

## Contributing

Contributions are welcome! To get started:

1. Fork this repository.
2. Create a new branch for your feature or fix.
3. Commit your changes with clear messages.
4. Open a pull request.

---

## License

This project is licensed under the [MIT License](LICENSE).