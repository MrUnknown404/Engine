using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using USharpLibs.Engine.Client.Fonts;
using USharpLibs.Engine.Client.GL;
using USharpLibs.Engine.Client.GL.Models;
using USharpLibs.Engine.Client.GL.Models.Vertex;
using USharpLibs.Engine.Client.GL.Shaders;

namespace USharpLibs.Engine.Client.UI.Elements {
	[PublicAPI]
	public class TextElement : UiElement {
		private string text = string.Empty;
		private Font? font;
		private bool isDirty = true;

		protected MutableModel<Vertex5> TextModel { get; } = new(BufferUsageHint.DynamicDraw);

		public bool DrawOutline { get; set; } = true;
		public bool DrawFont { get; set; } = true;
		public Color4 OutlineColor { get; set; } = Color4.Black;
		public Color4 FontColor { get; set; } = Color4.White;
		public byte OutlineSize { get; set; } = 150;

		public string Text {
			get => text;
			set {
				string oldText = Text;
				text = value;

				if (oldText != Text) { isDirty = true; }
			}
		}

		public Font? Font {
			get => font;
			set {
				Font? oldFont = Font;
				font = value;

				if (oldFont != Font) { isDirty = true; }
			}
		}

		public TextElement(short x, short y, float z) : base(x, y, z) { }

		public override void SetupGL() => TextModel.SetupGL();

		public override void Render(ShaderWriter shader, double time) {
			if (Font == null) { return; }
			if (isDirty) {
				TextModel.SetMesh(Font.GetMesh(Text, Font.Padding));
				isDirty = false;
			}

			if (Text.Length != 0 && (DrawOutline || DrawFont)) {
				GLH.Bind(Font.FontTexture);
				GLH.Bind(TextModel);
				GLH.DrawModel();
			}
		}

		public float GetTextWidth() => Font?.GetWidth(Text) ?? throw new NullReferenceException();
	}
}