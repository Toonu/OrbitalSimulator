using Godot;
using System;
using System.Collections.Generic;

namespace OrbitalSimulator.src.Orbits {
    [Tool]
    public partial class Orbit : MeshInstance3D {
        [Export] public Node3D Focus { get; set; }

        private Node3D _satellite;
        [Export]
        public Node3D Satellite {
            get => _satellite;
            set {
                _satellite = value;
                if (_satellite != null && _satellite.GetParent() != this)
                    GD.Print("Warning: Satellite should be a child of this Orbit node.");
                UpdateOrbit();
            }
        }

        private bool _visibleInEditor = true;
        [Export] public bool VisibleInEditor { get => _visibleInEditor; set { _visibleInEditor = value; UpdateOrbit(); } }

        private bool _filled = false;
        [Export] public bool Filled { get => _filled; set { _filled = value; UpdateOrbit(); } }

        private bool _simulate = false;
        [Export]
        public bool Simulate {
            get => _simulate;
            set { _simulate = value; _simulationTime = 0f; UpdateOrbit(); }
        }

        [Export(PropertyHint.Range, "0.01,100,0.1")]
        public float SimulationSpeed { get; set; } = 10.0f;   // Higher default for visibility

        private float _semiMajorAxis = 8f;
        [Export] public float SemiMajorAxis { get => _semiMajorAxis; set { _semiMajorAxis = value; UpdateOrbit(); } }

        private float _eccentricity = 0.2f;
        [Export(PropertyHint.Range, "0,2,0.01,or_greater")]
        public float Eccentricity { get => _eccentricity; set { _eccentricity = value; UpdateOrbit(); } }

        private float _inclination = 0.3f;
        [Export(PropertyHint.Range, "0,360,0.1,radians_as_degrees")]
        public float Inclination { get => _inclination; set { _inclination = value; UpdateOrbit(); } }

        private float _raan = 0.5f;
        [Export(PropertyHint.Range, "0,360,0.1,radians_as_degrees")]
        public float RightAscensionOfAscendingNode { get => _raan; set { _raan = value; UpdateOrbit(); } }

        private float _argumentOfPeriapsis = 1.0f;
        [Export(PropertyHint.Range, "0,360,0.1,radians_as_degrees")]
        public float ArgumentOfPeriapsis { get => _argumentOfPeriapsis; set { _argumentOfPeriapsis = value; UpdateOrbit(); } }

        private float _trueAnomaly = 0.0f;
        [Export(PropertyHint.Range, "0,360,0.1,radians_as_degrees")]
        public float TrueAnomaly { get => _trueAnomaly; set { _trueAnomaly = value; UpdateOrbit(); } }

        [Export] public int Segments { get; set; } = 200;
        [Export] public Color LineColor { get; set; } = Colors.Cyan;
        [Export] public Color FillColor { get; set; } = new Color(0.2f, 0.6f, 1.0f, 0.15f);

        private ImmediateMesh _immediateMesh;
        private float _simulationTime = 0f;

        public override void _Ready() {
            Focus ??= GetParent() as Node3D;
            UpdateOrbit();
        }

        public override void _Process(double delta) {
            if (Simulate && !Engine.IsEditorHint()) {
                _simulationTime += (float)delta * SimulationSpeed;
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
            var drawParams = new OrbitalParameters(
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

            if (_immediateMesh == null)
                _immediateMesh = new ImmediateMesh();
            else
                _immediateMesh.ClearSurfaces();

            if (Filled) {
                DrawFilledOrbit(points);
                DrawLineOrbit(points);
            } else {
                DrawLineOrbit(points);
            }

            Mesh = _immediateMesh;

            UpdateSatellitePosition();
        }

        private void UpdateSatellitePosition() {
            if (Satellite == null) return;

            var paramsNow = new OrbitalParameters(
                Focus.GlobalPosition,
                SemiMajorAxis,
                Eccentricity,
                TrueAnomaly,
                Inclination,
                RightAscensionOfAscendingNode,
                ArgumentOfPeriapsis
            );

            try {
                var (worldPos, _) = OrbitalMath.CalculateOrbitalVectorsFromParameters(paramsNow);
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
                TrueAnomaly,           // This is the starting True Anomaly
                Inclination,
                RightAscensionOfAscendingNode,
                ArgumentOfPeriapsis
            );

            try {
                var (worldPos, _) = OrbitalPropagator.Propagate(initialParams, _simulationTime);

                Satellite.Position = worldPos - GlobalPosition;
            } catch (Exception ex) {
                GD.PrintErr("Propagation failed: ", ex.Message);
            }
        }

        public void ClearOrbit() {
            _immediateMesh?.ClearSurfaces();
        }

        // ==================== Drawing Methods ====================
        private void DrawLineOrbit(List<Vector3> points) {
            _immediateMesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip);
            _immediateMesh.SurfaceSetColor(LineColor);
            foreach (var p in points)
                _immediateMesh.SurfaceAddVertex(p);
            _immediateMesh.SurfaceEnd();

            ApplyLineMaterial();
        }

        private void DrawFilledOrbit(List<Vector3> points) {
            _immediateMesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
            _immediateMesh.SurfaceSetColor(FillColor);

            Vector3 center = Focus.GlobalPosition - GlobalPosition;

            for (int i = 0; i < points.Count - 1; i++) {
                _immediateMesh.SurfaceAddVertex(center);
                _immediateMesh.SurfaceAddVertex(points[i] - GlobalPosition);
                _immediateMesh.SurfaceAddVertex(points[i + 1] - GlobalPosition);
            }

            _immediateMesh.SurfaceEnd();

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