using System.Numerics;
using ImGuiNET;

namespace Engine3.Client.Graphics.ImGui.Makers {
	public class CameraImGuiMaker : IImGuiMaker<Camera> {
		private CameraImGuiMaker() { }

		public static void ShowImGui(Camera obj) {
			// transform
			ImGuiNet.SeparatorText("Transform");

			Vector3 position = obj.Transform.Position;
			if (ImGuiNet.DragFloat3("Position", ref position, 0.1f / 2f)) { obj.Transform.Position = position; } // why x2?
			ImGuiH.HelpMarker("X/Y/Z");

			Vector3 rotation = new(obj.PitchDegrees, obj.YawDegrees, 0); // TODO roll
			if (ImGuiNet.DragFloat3("Rotation", ref rotation, 0.1f / 2f)) {
				obj.PitchDegrees = rotation.X;
				obj.YawDegrees = rotation.Y;
				// Object.RollDegrees = camPos.Z;
			}

			ImGuiH.HelpMarker("Pitch/Yaw/Roll (roll not implemented)");

			Vector3 forward = obj.Forward;
			ImGuiNet.InputFloat3("Forward", ref forward, null, ImGuiInputTextFlags.ReadOnly);
			ImGuiH.HelpMarker("X/Y/Z");

			// look at
			ImGuiNet.Separator();

			bool useLookAtPosition = obj.UseLookAtPosition;
			if (ImGuiNet.Checkbox("Use Look At Position", ref useLookAtPosition)) { obj.UseLookAtPosition = useLookAtPosition; }

			Vector3 lookAtPosition = obj.LookAtPosition;
			if (ImGuiNet.DragFloat3("Look At Position", ref lookAtPosition, 0.1f / 2f)) { obj.LookAtPosition = lookAtPosition; }
			ImGuiH.HelpMarker("X/Y/Z");

			// camera type & type specific values
			ImGuiNet.Separator();

			ImGuiNet.Text($"Camera Type: {obj.CameraType}");
			switch (obj.CameraType) {
				case Camera.CameraTypes.Orthographic:
					float width = obj.OrthographicWidth;
					if (ImGuiNet.DragFloat("Width", ref width, 0.1f / 2f, 0.001f, ushort.MaxValue)) { obj.OrthographicWidth = width; }

					float height = obj.OrthographicHeight;
					if (ImGuiNet.DragFloat("Height", ref height, 0.1f / 2f, 0.001f, ushort.MaxValue)) { obj.OrthographicHeight = height; }
					break;
				case Camera.CameraTypes.Perspective:
					float aspectRatio = obj.PerspectiveAspectRatio;
					if (ImGuiNet.DragFloat("Aspect Ratio", ref aspectRatio, 0.05f, 0.001f, 100, null, ImGuiSliderFlags.Logarithmic)) { obj.PerspectiveAspectRatio = aspectRatio; } // TODO i don't know what realistic values are

					float fov = obj.PerspectiveFovDegrees;
					if (ImGuiNet.DragFloat("Field Of View", ref fov, 0.05f, 1, 179, "%.3f\u00B0", ImGuiSliderFlags.Logarithmic)) { obj.PerspectiveFovDegrees = fov; }
					break;
				default: throw new ArgumentOutOfRangeException();
			}

			// near/far plane
			const float NearFarPadding = 0.01f;

			float nearPlane = obj.NearPlane;
			if (ImGuiNet.DragFloat("Near Plane", ref nearPlane, 10f, 0.0001f, ushort.MaxValue - NearFarPadding, "%.4f", ImGuiSliderFlags.Logarithmic)) { obj.NearPlane = nearPlane; }

			float farPlane = obj.FarPlane;
			if (ImGuiNet.DragFloat("Far Plane", ref farPlane, 10f, nearPlane + NearFarPadding, ushort.MaxValue, null, ImGuiSliderFlags.Logarithmic)) { obj.FarPlane = farPlane; }

			if (nearPlane + NearFarPadding > obj.FarPlane) { obj.FarPlane = nearPlane + NearFarPadding; }
		}
	}
}