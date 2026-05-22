using System.Numerics;
using ImGuiNET;

namespace Engine3.Client.Graphics.ImGui.Makers;

public static class CameraImGuiMaker {
	public static void ShowImGui(Camera camera) {
		// transform
		ImGuiNet.SeparatorText("Transform");

		Vector3 position = camera.Position;
		if (ImGuiNet.DragFloat3("Position", ref position, 0.1f / 2f)) { camera.Position = position; } // why x2?
		ImGuiH.HelpMarker("X/Y/Z");

		Vector4 orientation = camera.Orientation.AsVector4();
		if (ImGuiNet.DragFloat4("Orientation", ref orientation, 0.1f / 2f)) { camera.Orientation = new(orientation.X, orientation.Y, orientation.Z, orientation.W); } // why x2?
		ImGuiH.HelpMarker("X/Y/Z/W");

		Vector3 forward = camera.Forward;
		ImGuiNet.InputFloat3("Forward", ref forward, null, ImGuiInputTextFlags.ReadOnly);
		ImGuiH.HelpMarker("X/Y/Z");

		Vector3 right = camera.Right;
		ImGuiNet.InputFloat3("Right", ref right, null, ImGuiInputTextFlags.ReadOnly);
		ImGuiH.HelpMarker("X/Y/Z");

		Vector3 up = camera.Up;
		ImGuiNet.InputFloat3("Up", ref up, null, ImGuiInputTextFlags.ReadOnly);
		ImGuiH.HelpMarker("X/Y/Z");

		// look at
		ImGuiNet.Separator();

		bool useLookAtPosition = camera.UseLookAtPosition;
		if (ImGuiNet.Checkbox("Use Look At Position", ref useLookAtPosition)) { camera.UseLookAtPosition = useLookAtPosition; }

		Vector3 lookAtPosition = camera.LookAtPosition;
		if (ImGuiNet.DragFloat3("Look At Position", ref lookAtPosition, 0.1f / 2f)) { camera.LookAtPosition = lookAtPosition; }
		ImGuiH.HelpMarker("X/Y/Z");

		// camera type & type specific values
		ImGuiNet.Separator();

		ImGuiNet.Text($"Camera Type: {camera.CameraType}");
		switch (camera.CameraType) {
			case Camera.CameraTypes.Orthographic:
				float width = camera.OrthographicWidth;
				if (ImGuiNet.DragFloat("Width", ref width, 0.1f / 2f, 0.001f, ushort.MaxValue)) { camera.OrthographicWidth = width; }

				float height = camera.OrthographicHeight;
				if (ImGuiNet.DragFloat("Height", ref height, 0.1f / 2f, 0.001f, ushort.MaxValue)) { camera.OrthographicHeight = height; }
				break;
			case Camera.CameraTypes.Perspective:
				float aspectRatio = camera.PerspectiveAspectRatio;
				if (ImGuiNet.DragFloat("Aspect Ratio", ref aspectRatio, 0.05f, 0.001f, 100, null, ImGuiSliderFlags.Logarithmic)) { camera.PerspectiveAspectRatio = aspectRatio; }

				float fov = camera.PerspectiveFovDegrees;
				if (ImGuiNet.DragFloat("Field Of View", ref fov, 0.05f, 1, 179, "%.3f\u00B0", ImGuiSliderFlags.Logarithmic)) { camera.PerspectiveFovDegrees = fov; }
				break;
			default: throw new ArgumentOutOfRangeException();
		}

		// near/far plane
		const float NearFarPadding = 0.01f;

		float nearPlane = camera.NearPlane;
		if (ImGuiNet.DragFloat("Near Plane", ref nearPlane, 10f, 0.0001f, ushort.MaxValue - NearFarPadding, "%.4f", ImGuiSliderFlags.Logarithmic)) { camera.NearPlane = nearPlane; }

		float farPlane = camera.FarPlane;
		if (ImGuiNet.DragFloat("Far Plane", ref farPlane, 10f, nearPlane + NearFarPadding, ushort.MaxValue, null, ImGuiSliderFlags.Logarithmic)) { camera.FarPlane = farPlane; }

		if (nearPlane + NearFarPadding > camera.FarPlane) { camera.FarPlane = nearPlane + NearFarPadding; }
	}
}