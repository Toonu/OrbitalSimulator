using Godot;
using System.Collections.Generic;
using System.Linq;

namespace OrbitalSimulator.src.Orbits {
    /// <summary>
    /// Represents the set of parameters that define an orbit around a central body, including geometry and orientation
    /// elements.
    /// </summary>
    /// <remarks>Orbital parameters describe the size, shape, and orientation of an orbit in three-dimensional
    /// space. This class encapsulates commonly used elements such as semi-major axis, eccentricity, inclination, and
    /// orientation angles. All values are expressed in the coordinate system and units consistent with the simulation
    /// or calculation context. This class is typically used in orbital mechanics calculations, trajectory analysis, or
    /// space simulation scenarios.</remarks>
    /// 
    public class OrbitalParameters {
        /// <summary>
        /// Standard gravitational parameter (μ = Mu) of Earth in km³/s²
        /// </summary>
        public const float Mu = 398600f;
        /// <summary>
        /// Classification of the orbit based on eccentricity:
        /// - Elliptical (0 ≤ e < 1)
        /// - Parabolic (0 = 1)
        /// - Hyperbolic (e > 1)
        /// </summary>
        public OrbitType Type { get; set; }
        /// <summary>
        /// Focus of the Ellipse (F)
        /// Position of the primary focus (central body) of the orbit.
        /// </summary>
        public Vector3 Focus { get; set; }
        /// <summary>
        /// Semi-major axis (a)
        /// Half the longest diameter of the elliptical orbit.
        /// Negative for hyperbolic orbits.
        /// </summary>
        public float SemiMajorAxis { get; set; }
        /// <summary>
        /// Semi-minor axis (b)
        /// Half the shortest diameter of the elliptical orbit.
        /// </summary>
        public float SemiMinorAxis { get; set; }
        /// <summary>
        /// Semi-latus rectum / Semi-parameter (p)
        /// Distance from the focus to the orbit measured perpendicular to the major axis.
        /// </summary>
        public float SemiParameter { get; set; }
        /// <summary>
        /// Eccentricity (e)
        /// Measures how elongated the orbit is.
        /// </summary>
        public float Eccentricity { get; set; }
        /// <summary>
        /// Inclination (i)
        /// Angle between the orbital plane and the reference plane (usually the equatorial plane).
        /// Range: 0° to 180°.
        /// </summary>
        public float Inclination { get; set; }
        /// <summary>
        /// Right Ascension of the Ascending Node (Ω / RAAN)
        /// Angle from the reference direction to the ascending node, measured in the reference plane.
        /// </summary>
        public float RightAscensionOfAscendingNode { get; set; }
        /// <summary>
        /// Argument of Periapsis (ω)
        /// Angle from the ascending node to the periapsis, measured in the orbital plane in the direction of motion.
        /// </summary>
        public float ArgumentOfPeriapsis { get; set; }
        /// <summary>
        /// Apoapsis radius (Ap/rₐ)
        /// Maximum distance from the focus
        /// </summary>
        public float Apoapsis { get; set; }
        /// <summary>
        /// Periapsis radius (Pe/rₚ)
        /// Minimum distance from the focus
        /// </summary>
        public float Periapsis { get; set; }
        /// <summary>
        /// Orbital period (T)
        /// Time required to complete one full revolution around the central body.
        /// Only defined for closed (elliptical) orbits.
        /// </summary>
        public float OrbitalPeriod { get; set; }
        /// <summary>
        /// True Anomaly (ν / θ)
        /// Angle measured at the focus from the periapsis to the current position of the body,
        /// in the direction of motion. Describes the actual geometric position on the orbit.
        /// </summary>
        public float TrueAnomaly { get; set; }
        /// <summary>
        /// Eccentric Anomaly (E)
        /// An angular parameter that defines the position of a body moving along orbit.
        /// Measured at the center of the ellipse between the orbit's periapsis and the current position of the body. 
        /// </summary>
        public float EccentricAnomaly { get; set; }
        /// <summary>
        /// Mean Anomaly (M)
        /// An angular distance from the pericenter which a body would have if it moved in a circular orbit, with constant speed, 
        /// in the same orbital period as the actual body in its elliptical orbit.
        /// Convenient uniform measure of how far around its orbit a body has progressed since pericenter.
        /// </summary>
        public float MeanAnomaly { get; set; }
        /// <summary>
        /// Flight Path Angle (γ / ϕ)
        /// Angle between the velocity vector and the local horizontal (perpendicular to the radius vector). 
        /// Positive when the body is ascending.
        /// </summary>
        public float FlightPathAngle { get; set; }
        /// <summary>
        /// Specific Orbital Energy (ε)
        /// Total mechanical energy per unit mass (kinetic + potential).
        /// Negative for elliptical orbits, zero for parabolic, positive for hyperbolic.
        /// </summary>
        public float SpecificOrbitalEnergy { get; set; }
        /// <summary>
        /// Specific Angular Momentum (h)
        /// Angular momentum per unit mass. Magnitude of the cross product r × v.
        /// Constant for a given orbit at any point.
        /// </summary>
        public float SpecificAngularMomentum { get; set; }

        /*
            PQW = Perifocal frame (also called Perifocal Coordinate System)
            This is the natural orbital plane frame:
            P-axis: points toward periapsis
            Q-axis: perpendicular to P in the orbital plane (in direction of motion)
            W-axis: along the specific angular momentum vector h (normal to the orbital plane)

            ECI = Earth-Centered Inertial frame
            This is the "global" non-rotating inertial frame:
            Origin at center of Earth
            Z-axis along Earth's rotation axis
            X-axis toward vernal equinox (First Point of Aries)
            Y-axis completes the right-handed system
         */


        public OrbitalParameters(Vector3 centralBody, float semiMajorAxis, float eccentricity, float trueAnomaly, float inclination, float rightAscensionOfAscendingNode, float argumentOfPeriapsis) {
            Focus = centralBody;
            SemiMajorAxis = semiMajorAxis;
            Eccentricity = eccentricity;
            TrueAnomaly = trueAnomaly;
            Inclination = inclination;
            RightAscensionOfAscendingNode = rightAscensionOfAscendingNode;
            ArgumentOfPeriapsis = argumentOfPeriapsis;

            RecalculateDerivedValues();
        }


        private void RecalculateDerivedValues() {
            Type = OrbitalMath.GetOrbitType(Eccentricity);
            SemiParameter = OrbitalMath.CalculateSemiParameter(Eccentricity, SemiMajorAxis);
            if (Type == OrbitType.Elliptical) {
                //Fails for hyperbolic orbits, so we catch the exception and leave SemiMinorAxis as 0 in that case.
                SemiMinorAxis = OrbitalMath.CalculateSemiMinorAxis(Eccentricity, SemiMajorAxis);
            }


            Periapsis = OrbitalMath.CalculatePeriapsis(Eccentricity, SemiMajorAxis);
            //Hyperbolic orbit does not have Apoapsis, so we set it to NaN in that case. Otherwise, we calculate it normally.
            Apoapsis = Type != OrbitType.Elliptical ? float.NaN : OrbitalMath.CalculateApoapsis(Eccentricity, SemiMajorAxis);

            OrbitalPeriod = Type == OrbitType.Elliptical ? OrbitalMath.CalculateOrbitalPeriod(SemiMajorAxis, Mu) : float.NaN;

            EccentricAnomaly = OrbitalMath.CalculateEccentricAnomaly(Eccentricity, TrueAnomaly);
            MeanAnomaly = OrbitalMath.CalculateMeanAnomaly(EccentricAnomaly, Eccentricity);

            SpecificOrbitalEnergy = OrbitalMath.CalculateSpecificOrbitalEnergy(SemiMajorAxis);
            SpecificAngularMomentum = OrbitalMath.CalculateSpecificAngularMomentum(SemiParameter);
            var (r, v) = OrbitalMath.CalculateOrbitalVectorsFromParameters(this);
            FlightPathAngle = OrbitalMath.CalculateFlightPathAngle(r, v);
        }


        public override bool Equals(object other) {
            if (other is OrbitalParameters otherParams) {
                return Eccentricity == otherParams.Eccentricity &&
                       SemiMajorAxis == otherParams.SemiMajorAxis &&
                       Inclination == otherParams.Inclination &&
                       RightAscensionOfAscendingNode == otherParams.RightAscensionOfAscendingNode &&
                       ArgumentOfPeriapsis == otherParams.ArgumentOfPeriapsis &&
                       TrueAnomaly == otherParams.TrueAnomaly;
            }
            return false;
        }


        public override string ToString() {
            List<string> values = [];
            values.Add($"[ORBIT]");
            values.Add($"F = {Focus}");
            values.Add($"a = {SemiMajorAxis}km");
            values.Add($"b = {SemiMinorAxis}km");
            values.Add($"p = {SemiParameter}km");
            values.Add($"ν = {OrbitalMath.ToDegrees(TrueAnomaly)}°");
            values.Add($"i = {OrbitalMath.ToDegrees(Inclination)}°");
            values.Add($"Ω = {OrbitalMath.ToDegrees(RightAscensionOfAscendingNode)}°");
            values.Add($"ω = {OrbitalMath.ToDegrees(ArgumentOfPeriapsis)}°");
            values.Add($"φ = {OrbitalMath.ToDegrees(FlightPathAngle)}°");
            values.Add($"ε = {SpecificOrbitalEnergy}km²/s²");
            values.Add($"h = {SpecificAngularMomentum}km²/s²");
            return string.Join('\n', values);
        }


        public override int GetHashCode() {
            return Eccentricity.GetHashCode() ^ SemiMajorAxis.GetHashCode() ^ Inclination.GetHashCode() ^ RightAscensionOfAscendingNode.GetHashCode() ^ ArgumentOfPeriapsis.GetHashCode() ^ TrueAnomaly.GetHashCode();
        }
    }
}
