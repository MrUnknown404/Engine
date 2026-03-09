global using ZLinq;
global using GlBool = OpenTK.Graphics.OpenGL.Boolean; // ambiguous reference. System contains type with the same name
global using GlShaderType = OpenTK.Graphics.OpenGL.ShaderType; // i have a type with the same name
global using ImGuiNet = ImGuiNET.ImGui; // i have a namespace with the same name

[assembly: ZLinqDropIn("Engine3", DropInGenerateTypes.Collection)]