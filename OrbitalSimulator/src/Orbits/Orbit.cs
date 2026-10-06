using Godot;
using System;
using System.Collections.Generic;

namespace OrbitalSimulator.src.Orbits {
    /// <summary>
    /// A Godot <see cref="MeshInstance3D"/> that renders a Keplerian orbit (elliptical, parabolic, or hyperbolic)
    /// around a central body and optionally animates a satellite node along it over time.
    /// </summary>
    /// <remarks>Marked with <see cref="ToolAttribute"/> so the orbit path and satellite position update live in
    /// the editor as properties are changed, not just at runtime.</remarks>
    [Tool]
    public partial class Orbit : MeshInstance3D {
        /// <summary>
        /// The central body (focus) this orbit revolves around. Its <see cref="Node3D.GlobalPosition"/> is used as
        /// the focus of the ellipse/hyperbola/parabola. Defaults to the parent node if left unset.
        /// </summary>
        [Export] private Node3D Focus { get; set; }

        /// <summary>
        /// Optional labels used to display the current orbital parameters (see <see cref="OrbitalParameters.ToString"/>) for debugging/inspection.
        /// </summary>
        [Export] private Godot.Collections.Array<Label> InfoLabels { get; set; }
        private Node3D satellite;
        /// <summary>
        /// The node representing the orbiting body. Its local position is updated to reflect the current point on
        /// the orbit, either statically (<see cref="TrueAnomaly"/>) or dynamically while <see cref="Simulate"/> is enabled.
        /// </summary>
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
        /// <summary>
        /// Whether the orbit path mesh should be drawn while editing in the Godot editor.
        /// </summary>
        [Export] public bool VisibleInEditor { get => visibleInEditor; set { visibleInEditor = value; UpdateOrbit(); } }

        private bool filled = false;
        /// <summary>
        /// Whether to render a filled/shaded triangle fan for the orbit's enclosed area in addition to its outline.
        /// </summary>
        [Export] public bool Filled { get => filled; set { filled = value; UpdateOrbit(); } }

        private bool simulate = false;
        /// <summary>
        /// Enables time-based propagation of the satellite's position along the orbit during <see cref="_Process(double)"/>.
        /// Toggling this resets <see cref="simulationTime"/> back to zero.
        /// </summary>
        [Export]
        public bool Simulate {
            get => simulate;
            set { simulate = value; simulationTime = 0f; UpdateOrbit(); }
        }

        /// <summary>
        /// Multiplier applied to elapsed real time when propagating the satellite, in simulated seconds per real second.
        /// </summary>
        [Export(PropertyHint.Range, "0.01,100,0.1")]
        public float SimulationSpeed { get; set; } = 10.0f;

        private float semiMajorAxis = 8f;
        /// <summary>
        /// Semi-major axis magnitude (a), in the same distance units as the scene, as entered by the user.
        /// Always treated as a positive magnitude in the inspector; the correctly signed value (negative for
        /// hyperbolic orbits) is derived internally via <see cref="GetSignedSemiMajorAxis"/> before being passed
        /// to <see cref="OrbitalMath"/>/<see cref="OrbitalParameters"/>, which expect a signed semi-major axis.
        /// </summary>
        [Export] public float SemiMajorAxis { get => semiMajorAxis; set { semiMajorAxis = value; UpdateOrbit(); } }

        private float eccentricity = 0.2f;
        /// <summary>
        /// Orbital eccentricity (e). Values in [0, 1) produce an elliptical orbit, e = 1 a parabolic orbit, and
        /// e > 1 a hyperbolic orbit (see <see cref="OrbitType"/>).
        /// </summary>
        [Export(PropertyHint.Range, "0,2,0.01,or_greater")]
        public float Eccentricity { get => eccentricity; set { eccentricity = value; UpdateOrbit(); } }

        private float inclination = 0.3f;
        /// <summary>
        /// Orbital inclination (i), in radians, between the orbital plane and the reference (XZ) plane.
        /// </summary>
        [Export(PropertyHint.Range, "0,360,0.1,radians_as_degrees")]
        public float Inclination { get => inclination; set { inclination = value; UpdateOrbit(); } }

        private float raan = 0.5f;
        /// <summary>
        /// Right Ascension of the Ascending Node (Ω), in radians, orienting the ascending node within the reference plane.
        /// </summary>
        [Export(PropertyHint.Range, "0,360,0.1,radians_as_degrees")]
        public float RightAscensionOfAscendingNode { get => raan; set { raan = value; UpdateOrbit(); } }

        private float argumentOfPeriapsis = 1.0f;
        /// <summary>
        /// Argument of Periapsis (ω), in radians, orienting the periapsis within the orbital plane relative to the ascending node.
        /// </summary>
        [Export(PropertyHint.Range, "0,360,0.1,radians_as_degrees")]
        public float ArgumentOfPeriapsis { get => argumentOfPeriapsis; set { argumentOfPeriapsis = value; UpdateOrbit(); } }

        private float trueAnomaly = 0.0f;
        /// <summary>
        /// True anomaly (ν), in radians, giving the satellite's initial position along the orbit measured from periapsis.
        /// </summary>
        [Export(PropertyHint.Range, "0,360,0.1,radians_as_degrees")]
        public float TrueAnomaly { get => trueAnomaly; set { trueAnomaly = value; UpdateOrbit(); } }

        /// <summary>
        /// Number of line segments used to approximate the orbit path mesh. Higher values produce a smoother curve at a higher rendering cost.
        /// </summary>
        [Export] public int Segments { get; set; } = 200;
        /// <summary>
        /// Color of the orbit's outline.
        /// </summary>
        [Export] public Color LineColor { get; set; } = Colors.Cyan;
        /// <summary>
        /// Color (including alpha) used for the optional filled area when <see cref="Filled"/> is enabled.
        /// </summary>
        [Export] public Color FillColor { get; set; } = new Color(0.2f, 0.6f, 1.0f, 0.15f);

        private ImmediateMesh immediateMesh;
        private float simulationTime = 0f;
        private OrbitalParameters orbitalParameters;

        /// <summary>
        /// Defaults <see cref="Focus"/> to the parent node if not explicitly assigned, then performs the initial orbit draw.
        /// </summary>
        public override void _Ready() {
            Focus ??= GetParent() as Node3D;
            UpdateOrbit();
        }

        /// <summary>
        /// Advances the simulation clock and propagates the satellite's position each frame while
        /// <see cref="Simulate"/> is enabled and the scene is running (not just being edited).
        /// </summary>
        /// <param name="delta">Elapsed real time, in seconds, since the previous frame.</param>
        public override void _Process(double delta) {
            if (Simulate && !Engine.IsEditorHint()) {
                simulationTime += (float)delta * SimulationSpeed;
                UpdateSatellitePositionFromTime();
            }
        }

        /// <summary>
        /// Rebuilds the orbit path mesh and refreshes the satellite position and info label from the current
        /// exported orbital element values. Safe to call whenever any orbital parameter changes; invalid
        /// combinations (e.g. inconsistent eccentricity/anomaly) are caught and simply clear the drawn orbit.
        /// </summary>
        public void UpdateOrbit() {
            if (Focus == null) return;

            // Draw the full static orbit
            if (!Focus.IsInsideTree()) return;

            float signedA = GetSignedSemiMajorAxis();

            OrbitalParameters drawParams;
            try {
                drawParams = new(
                    Focus.GlobalPosition,
                    signedA,
                    Eccentricity,
                    TrueAnomaly,
                    Inclination,
                    RightAscensionOfAscendingNode,
                    ArgumentOfPeriapsis
                );
            } catch (Exception ex) {
                GD.PrintErr("Failed to build orbital parameters: ", ex.Message);
                ClearOrbit();
                return;
            }

            var points = drawParams.Type switch {
                OrbitType.Elliptical => OrbitalPropagator.GenerateEllipsePoints(drawParams, Segments),
                OrbitType.Parabolic => OrbitalPropagator.GenerateParabolicPoints(drawParams, Segments),
                _ => OrbitalPropagator.GenerateHyperbolicPoints(drawParams, Segments)
            };

            if (immediateMesh == null) immediateMesh = new ImmediateMesh();
            else immediateMesh.ClearSurfaces();

            if (Filled) DrawFilledOrbit(points);
            DrawLineOrbit(points);

            Mesh = immediateMesh;

            UpdateSatellitePosition();
            UpdateInfoLabel();
        }

        /// <summary>
        /// Returns the semi-major axis with the sign expected by the orbital math, derived from the
        /// user-facing, always-positive/magnitude <see cref="SemiMajorAxis"/> value and the current
        /// <see cref="Eccentricity"/>: negative for hyperbolic orbits (e &gt; 1), positive otherwise.
        /// This keeps the Godot inspector value magnitude-only while satisfying the sign convention
        /// required by <c>OrbitalMath</c>.
        /// </summary>
        private float GetSignedSemiMajorAxis() {
            float magnitude = MathF.Abs(SemiMajorAxis);
            return Eccentricity > 1f ? -magnitude : magnitude;
        }

        /// <summary>
        /// Refreshes <see cref="InfoLabel"/> with the human-readable representation of the current <see cref="orbitalParameters"/>.
        /// </summary>
        private void UpdateInfoLabel() {
            foreach (Label infoLabel in InfoLabels) {
                infoLabel?.Text = orbitalParameters.ToString();
            }
        }

        /// <summary>
        /// Places <see cref="Satellite"/> at the static position corresponding to the current <see cref="TrueAnomaly"/>
        /// (i.e. without any time-based propagation). Used for editor preview and whenever exported values change.
        /// </summary>
        private void UpdateSatellitePosition() {
            if (Satellite == null) return;

            try {
                orbitalParameters = new OrbitalParameters(
                    Focus.GlobalPosition,
                    GetSignedSemiMajorAxis(),
                    Eccentricity,
                    TrueAnomaly,
                    Inclination,
                    RightAscensionOfAscendingNode,
                    ArgumentOfPeriapsis
                );

                var (worldPos, _) = OrbitalMath.CalculateOrbitalVectorsFromParameters(orbitalParameters);
                Satellite.Position = worldPos - GlobalPosition;
            } catch (Exception ex) {
                GD.PrintErr("Failed to update satellite: ", ex.Message);
            }
        }

        /// <summary>
        /// Places <see cref="Satellite"/> at the position reached after propagating forward by <see cref="simulationTime"/>
        /// seconds from the initial orbital elements, using <see cref="OrbitalPropagator.Propagate"/>.
        /// </summary>
        private void UpdateSatellitePositionFromTime() {
            if (Satellite == null || Focus == null) return;

            try {
                var initialParams = new OrbitalParameters(
                    Focus.GlobalPosition,
                    GetSignedSemiMajorAxis(),
                    Eccentricity,
                    TrueAnomaly,
                    Inclination,
                    RightAscensionOfAscendingNode,
                    ArgumentOfPeriapsis
                );

                var (worldPos, _) = OrbitalPropagator.Propagate(initialParams, simulationTime);

                Satellite.Position = worldPos - GlobalPosition;
            } catch (Exception ex) {
                GD.PrintErr("Propagation failed: ", ex.Message);
            }
        }

        /// <summary>
        /// Removes all drawn surfaces from the orbit mesh, leaving it empty (e.g. when the current orbital
        /// parameters are invalid and cannot be rendered).
        /// </summary>
        public void ClearOrbit() {
            immediateMesh?.ClearSurfaces();
        }

        // ==================== Drawing Methods ====================
        /// <summary>
        /// Draws the orbit outline as a line strip through the given world-space points, using <see cref="LineColor"/>.
        /// </summary>
        /// <param name="points">World-space positions along the orbit path, in drawing order.</param>
        private void DrawLineOrbit(List<Vector3> points) {
            immediateMesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip);
            immediateMesh.SurfaceSetColor(LineColor);
            foreach (var p in points) immediateMesh.SurfaceAddVertex(p);
            immediateMesh.SurfaceEnd();

            ApplyLineMaterial();
        }

        /// <summary>
        /// Draws a filled triangle fan spanning from the focus to each consecutive pair of orbit points, using <see cref="FillColor"/>.
        /// </summary>
        /// <param name="points">World-space positions along the orbit path, in drawing order.</param>
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

        /// <summary>
        /// Ensures an unshaded, unculled material configured for <see cref="LineColor"/> is applied as the mesh's override.
        /// Only creates the material once; a filled material set by <see cref="ApplyFillMaterial"/> is not overwritten.
        /// </summary>
        private void ApplyLineMaterial() {
            MaterialOverride ??= new StandardMaterial3D {
                AlbedoColor = LineColor,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                VertexColorUseAsAlbedo = true,
                Transparency = BaseMaterial3D.TransparencyEnum.Disabled,
                CullMode = BaseMaterial3D.CullModeEnum.Disabled
            };
        }

        /// <summary>
        /// Configures (or reuses) the mesh's override material for an unshaded, alpha-blended, unculled fill using <see cref="FillColor"/>.
        /// </summary>
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
