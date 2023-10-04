using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using USharpLibs.Engine.Client.Fonts;
using USharpLibs.Engine.Client.GL;
using USharpLibs.Engine.Client.GL.Models;

namespace USharpLibs.Engine.Client.UI.Elements {
	[PublicAPI]
	public class TextElement : UiElement {
		private string text = string.Empty;
		private Font? font;
		private bool isDirty = true;

		protected UnboundModel<DynamicModel> TextModel { get; } = new(new(BufferUsageHint.DynamicDraw));

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

		protected override void ISetupGL() => TextModel.SetupGL();

		public override void Render(Shader shader, double time) {
			if (isDirty && Font != null) {
				TextModel.Model.SetMesh(Font.GetMesh(Text, Font.Padding)).RefreshModelData();
				isDirty = false;
			}

			if (Text.Length != 0 && (DrawOutline || DrawFont)) { GLH.Bind(TextModel)?.Draw(); }
		}
	}
}