using Godot;

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
        public const float mu = 398600f; //μ = km³/s² for Earth
        public Vector3 Focus { get; set; } //F
        public float SemiMajorAxis { get; set; } //a
        public float SemiMinorAxis { get; set; } //b
        public float SemiParameter { get; set; } //p
        public float Eccentricity { get; set; } //e
        public float Inclination { get; set; } //i
        public float RightAscensionOfAscendingNode { get; set; } //Ω = RAAN
        public float ArgumentOfPeriapsis { get; set; } //ω = Omega
        public float Apoapsis { get; set; } //AP = r_ap = r_max, farthest point from the central body.
        public float Periapsis { get; set; } //Pe = r_pe = r_min, closest point from the central body.
        public OrbitType Type { get; set; }
        public float OrbitalPeriod { get; set; } //T
        /// <summary>
        /// True Anomaly = Nu = ν = θ
        /// An angular parameter that defines the position of a body moving along orbit. 
        /// Measured at the focus of the ellipse between the orbit's periapsis and the current position of the body.
        /// </summary>
        public float TrueAnomaly { get; set; }
        /// <summary>
        /// Eccentric Anomaly = E
        /// An angular parameter that defines the position of a body moving along orbit.
        /// Measured at the center of the ellipse between the orbit's periapsis and the current position of the body. 
        /// </summary>
        public float EccentricAnomaly { get; set; }

        /// <summary>
        /// Mean Anomaly = M
        /// An angular distance from the pericenter which a body would have if it moved in a circular orbit, with constant speed, 
        /// in the same orbital period as the actual body in its elliptical orbit.
        /// Convenient uniform measure of how far around its orbit a body has progressed since pericenter.
        /// </summary>
        public float MeanAnomaly { get; set; }
        public float FlightPathAngle { get; set; } //φ
        public float SpecificOrbitalEnergy { get; set; } //ε
        public float SpecificAngularMomentum { get; set; } //h

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
            try {
                //Fails for hyperbolic orbits, so we catch the exception and leave SemiMinorAxis as 0 in that case.
                SemiMinorAxis = OrbitalMath.CalculateSemiMinorAxis(Eccentricity, SemiMajorAxis);
            } catch { }


            Periapsis = OrbitalMath.CalculatePeriapsis(Eccentricity, SemiMajorAxis);
            //Hyperbolic orbit does not have Apoapsis, so we set it to NaN in that case. Otherwise, we calculate it normally.
            Apoapsis = Type == OrbitType.Hyperbolic ? float.NaN : OrbitalMath.CalculateApoapsis(Eccentricity, SemiMajorAxis);

            OrbitalPeriod = Type == OrbitType.Elliptical ? OrbitalMath.CalculateOrbitalPeriod(SemiMajorAxis, mu) : float.NaN;

            if (EccentricAnomaly == 0) EccentricAnomaly = OrbitalMath.CalculateEccentricAnomaly(Eccentricity, TrueAnomaly);
            if (MeanAnomaly == 0) MeanAnomaly = OrbitalMath.CalculateMeanAnomaly(EccentricAnomaly, Eccentricity);

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
            return $"[ORBIT]\n" +
                $"F = {Focus}\n" +
                $"a = {SemiMajorAxis}m\n" +
                $"b = {SemiMinorAxis}m\n" +
                $"p = {SemiParameter}m\n" +
                $"v = {OrbitalMath.ToDegrees(TrueAnomaly)}°\n" +
                $"i = {OrbitalMath.ToDegrees(Inclination)}°\n" +
                $"Ω = {OrbitalMath.ToDegrees(RightAscensionOfAscendingNode)}°\n" +
                $"ω = {OrbitalMath.ToDegrees(ArgumentOfPeriapsis)}°\n" +
                $"ε = {OrbitalMath.ToDegrees(SpecificOrbitalEnergy)}km²/s²\n" +
                $"h = {OrbitalMath.ToDegrees(SpecificAngularMomentum)}km/s²\n" +
                $"φ = {OrbitalMath.ToDegrees(FlightPathAngle)}°";

        }

        public override int GetHashCode() {
            return Eccentricity.GetHashCode() ^ SemiMajorAxis.GetHashCode() ^ Inclination.GetHashCode() ^ RightAscensionOfAscendingNode.GetHashCode() ^ ArgumentOfPeriapsis.GetHashCode() ^ TrueAnomaly.GetHashCode();
        }
    }
}
