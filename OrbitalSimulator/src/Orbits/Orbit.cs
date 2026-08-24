using Godot;
using System;
using System.Collections.Generic;

namespace OrbitalSimulator.src.Orbits {
    [Tool]
    public partial class Orbit : MeshInstance3D {
        [Export] private Node3D Focus { get; set; }
        [Export] private Label InfoLabel { get; set; }
        private Node3D satellite;
        [Export]
        public Node3D Satellite {
            get => satellite;
            set {
                satellite = value;
                if (satellite != null && satellite.GetParent() != this) GD.Print("Warning: Satellite should be a child of this Orbit node.");
                UpdateOrbit();
            }
        }

        private bool visibleInEditor = true;
        [Export] public bool VisibleInEditor { get => visibleInEditor; set { visibleInEditor = value; UpdateOrbit(); } }

        private bool filled = false;
        [Export] public bool Filled { get => filled; set { filled = value; UpdateOrbit(); } }

        private bool simulate = false;
        [Export]
        public bool Simulate {
            get => simulate;
            set { simulate = value; simulationTime = 0f; UpdateOrbit(); }
        }

        [Export(PropertyHint.Range, "0.01,100,0.1")]
        public float SimulationSpeed { get; set; } = 10.0f;   // Higher default for visibility

        private float semiMajorAxis = 8f;
        [Export] public float SemiMajorAxis { get => semiMajorAxis; set { semiMajorAxis = value; UpdateOrbit(); } }

        private float eccentricity = 0.2f;
        [Export(PropertyHint.Range, "0,2,0.01,or_greater")]
        public float Eccentricity { get => eccentricity; set { eccentricity = value; UpdateOrbit(); } }

        private float inclination = 0.3f;
        [Export(PropertyHint.Range, "0,360,0.1,radians_as_degrees")]
        public float Inclination { get => inclination; set { inclination = value; UpdateOrbit(); } }

        private float raan = 0.5f;
        [Export(PropertyHint.Range, "0,360,0.1,radians_as_degrees")]
        public float RightAscensionOfAscendingNode { get => raan; set { raan = value; UpdateOrbit(); } }

        private float argumentOfPeriapsis = 1.0f;
        [Export(PropertyHint.Range, "0,360,0.1,radians_as_degrees")]
        public float ArgumentOfPeriapsis { get => argumentOfPeriapsis; set { argumentOfPeriapsis = value; UpdateOrbit(); } }

        private float trueAnomaly = 0.0f;
        [Export(PropertyHint.Range, "0,360,0.1,radians_as_degrees")]
        public float TrueAnomaly { get => trueAnomaly; set { trueAnomaly = value; UpdateOrbit(); } }

        [Export] public int Segments { get; set; } = 200;
        [Export] public Color LineColor { get; set; } = Colors.Cyan;
        [Export] public Color FillColor { get; set; } = new Color(0.2f, 0.6f, 1.0f, 0.15f);

        private ImmediateMesh immediateMesh;
        private float simulationTime = 0f;
        private OrbitalParameters orbitalParameters;

        public override void _Ready() {
            Focus ??= GetParent() as Node3D;
            UpdateOrbit();
        }

        public override void _Process(double delta) {
            if (Simulate && !Engine.IsEditorHint()) {
                simulationTime += (float)delta * SimulationSpeed;
                UpdateSatellitePositionFromTime();
            }
        }

        public void UpdateOrbit() {
            if (Focus == null) return;

            if (Eccentricity == 1f) {
                GD.Print("Parabolic orbits are not supported.");
                ClearOrbit();
                return;
            }

            // Draw the full static orbit
            if (!Focus.IsInsideTree()) return;
            OrbitalParameters drawParams = new(
                Focus.GlobalPosition,
                SemiMajorAxis,
                Eccentricity,
                TrueAnomaly,
                Inclination,
                RightAscensionOfAscendingNode,
                ArgumentOfPeriapsis
            );

            var points = drawParams.Type != OrbitType.Elliptical
                ? OrbitalPropagator.GenerateHyperbolicPoints(drawParams, Segments)
                : OrbitalPropagator.GenerateEllipsePoints(drawParams, Segments);

            if (immediateMesh == null) immediateMesh = new ImmediateMesh();
            else immediateMesh.ClearSurfaces();

            if (Filled) {
                DrawFilledOrbit(points);
                DrawLineOrbit(points);
            } else {
                DrawLineOrbit(points);
            }

            Mesh = immediateMesh;

            UpdateSatellitePosition();
            UpdateInfoLabel();
        }

        private void UpdateInfoLabel() {
            InfoLabel?.Text = orbitalParameters.ToString();
        }

        private void UpdateSatellitePosition() {
            if (Satellite == null) return;

            orbitalParameters = new OrbitalParameters(
                Focus.GlobalPosition,
                SemiMajorAxis,
                Eccentricity,
                TrueAnomaly,
                Inclination,
                RightAscensionOfAscendingNode,
                ArgumentOfPeriapsis
            );

            try {
                var (worldPos, _) = OrbitalMath.CalculateOrbitalVectorsFromParameters(orbitalParameters);
                Satellite.Position = worldPos - GlobalPosition;
            } catch (Exception ex) {
                GD.PrintErr("Failed to update satellite: ", ex.Message);
            }
        }

        private void UpdateSatellitePositionFromTime() {
            if (Satellite == null || Focus == null) return;

            var initialParams = new OrbitalParameters(
                Focus.GlobalPosition,
                SemiMajorAxis,
                Eccentricity,
                TrueAnomaly,
                Inclination,
                RightAscensionOfAscendingNode,
                ArgumentOfPeriapsis
            );

            try {
                var (worldPos, _) = OrbitalPropagator.Propagate(initialParams, simulationTime);

                Satellite.Position = worldPos - GlobalPosition;
            } catch (Exception ex) {
                GD.PrintErr("Propagation failed: ", ex.Message);
            }
        }

        public void ClearOrbit() {
            immediateMesh?.ClearSurfaces();
        }

        // ==================== Drawing Methods ====================
        private void DrawLineOrbit(List<Vector3> points) {
            immediateMesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip);
            immediateMesh.SurfaceSetColor(LineColor);
            foreach (var p in points)
                immediateMesh.SurfaceAddVertex(p);
            immediateMesh.SurfaceEnd();

            ApplyLineMaterial();
        }

        private void DrawFilledOrbit(List<Vector3> points) {
            immediateMesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
            immediateMesh.SurfaceSetColor(FillColor);

            Vector3 center = Focus.GlobalPosition - GlobalPosition;

            for (int i = 0; i < points.Count - 1; i++) {
                immediateMesh.SurfaceAddVertex(center);
                immediateMesh.SurfaceAddVertex(points[i] - GlobalPosition);
                immediateMesh.SurfaceAddVertex(points[i + 1] - GlobalPosition);
            }

            immediateMesh.SurfaceEnd();

            ApplyFillMaterial();
        }

        private void ApplyLineMaterial() {
            MaterialOverride ??= new StandardMaterial3D {
                AlbedoColor = LineColor,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                VertexColorUseAsAlbedo = true,
                Transparency = BaseMaterial3D.TransparencyEnum.Disabled,
                CullMode = BaseMaterial3D.CullModeEnum.Disabled
            };
        }

        private void ApplyFillMaterial() {
            var mat = MaterialOverride as StandardMaterial3D ?? new StandardMaterial3D();
            MaterialOverride = mat;
            mat.AlbedoColor = FillColor;
            mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
            mat.VertexColorUseAsAlbedo = true;
            mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
            mat.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        }
    }
}
