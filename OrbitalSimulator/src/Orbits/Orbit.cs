using Godot;
using System;
using System.Collections.Generic;

namespace OrbitalSimulator.src.Orbits {
    [Tool]
    public partial class Orbit : MeshInstance3D {
        [Export] public Node3D Focus { get; set; }

        private bool _visibleInEditor = true;
        [Export]
        public bool VisibleInEditor {
            get => _visibleInEditor;
            set {
                _visibleInEditor = value;
                UpdateOrbit();
            }
        }

        private float _semiMajorAxis = 8f;
        [Export]
        public float SemiMajorAxis {
            get => _semiMajorAxis;
            set { _semiMajorAxis = value; UpdateOrbit(); }
        }

        private float _eccentricity = 0.2f;
        [Export(PropertyHint.Range, "0,1.99,0.01")]
        public float Eccentricity {
            get => _eccentricity;
            set { _eccentricity = value; UpdateOrbit(); }
        }

        private float _inclination = 0.3f;
        [Export]
        public float Inclination {
            get => _inclination;
            set { _inclination = value; UpdateOrbit(); }
        }

        private float _raan = 0.5f;
        [Export]
        public float RightAscensionOfAscendingNode {
            get => _raan;
            set { _raan = value; UpdateOrbit(); }
        }

        private float _argumentOfPeriapsis = 1.0f;
        [Export]
        public float ArgumentOfPeriapsis {
            get => _argumentOfPeriapsis;
            set { _argumentOfPeriapsis = value; UpdateOrbit(); }
        }

        private float _trueAnomaly = 0.0f;
        [Export]
        public float TrueAnomaly {
            get => _trueAnomaly;
            set { _trueAnomaly = value; UpdateOrbit(); }
        }

        [Export] public int Segments { get; set; } = 200;
        [Export] public Color LineColor { get; set; } = Colors.Cyan;

        private ImmediateMesh _immediateMesh;

        public override void _Ready() {
            Focus ??= GetParent() as Node3D;
            UpdateOrbit();
        }


        public void ClearOrbit() {
            _immediateMesh?.ClearSurfaces();
            //Mesh = null;
        }

        public void UpdateOrbit() {
            if (Focus == null) {
                Console.WriteLine("Focus cannot be empty. Assign central body!");
                ClearOrbit();
                return;
            }
            if (Eccentricity == 1) {
                Console.WriteLine("Parabolic orbits are not supported.");
                ClearOrbit();
                return;
            }

            if (!VisibleInEditor) {
                ClearOrbit();
                return;
            }

            var parameters = new OrbitalParameters(Focus.GlobalPosition, SemiMajorAxis, Eccentricity, TrueAnomaly, Inclination, RightAscensionOfAscendingNode, ArgumentOfPeriapsis);

            List<Vector3> points = parameters.Type != OrbitType.Elliptical
                ? OrbitalPropagator.GenerateHyperbolicPoints(parameters, Segments)
                : OrbitalPropagator.GenerateEllipsePoints(parameters, Segments);

            if (points.Count < 3) return;

            if (_immediateMesh == null) {
                _immediateMesh = new ImmediateMesh();
            } else {
                ClearOrbit();
            }

            _immediateMesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip);
            _immediateMesh.SurfaceSetColor(LineColor);

            foreach (Vector3 p in points)
                _immediateMesh.SurfaceAddVertex(p);

            _immediateMesh.SurfaceEnd();

            if (MaterialOverride == null) {
                var material = new StandardMaterial3D {
                    AlbedoColor = LineColor,
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                    VertexColorUseAsAlbedo = true,
                    Transparency = BaseMaterial3D.TransparencyEnum.Disabled,
                    CullMode = BaseMaterial3D.CullModeEnum.Disabled
                };
                MaterialOverride = material;
            }
            Mesh = _immediateMesh;
        }
    }
}