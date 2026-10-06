using Godot;
using System;

namespace OrbitalSimulator.src.Orbits {
    public static partial class OrbitalMath {
        /// <summary>
        /// Calculates Semi Parameter
        /// </summary>
        /// <param name="e">Eccentricity</param>
        /// <param name="a">Semi-Major Axis</param>
        /// <returns>Semi Parameter / Semi Latus Rectum p</returns>
        /// <exception cref="ArgumentException">Thrown when a is zero, or when e and a have an inconsistent sign
        /// (a &gt; 0 with e &gt; 1, or a &lt; 0 with e &lt; 1).</exception>
        public static float CalculateSemiParameter(float e, float a) {
            if (a == 0) throw new ArgumentException("Semi-major axis cannot be zero");
            if (e < 0) throw new ArgumentException("Eccentricity cannot be negative");
            if (a > 0 && e > 1f) throw new ArgumentException("Semi-major axis must be negative for hyperbolic orbits (e > 1)");
            if (a < 0 && e < 1f) throw new ArgumentException("Semi-major axis must be positive for elliptical orbits (e < 1)");

            // p = a * (1 - e²). Signs cancel naturally: ellipse (a>0, 1-e²>0) and
            // hyperbola (a<0, 1-e²<0) both yield a positive p without needing Abs.
            return a * (1 - e * e);
        }


        /// <summary>
        /// Calculates the semi-latus rectum (semi-parameter) of a parabolic orbit (e = 1) from the periapsis distance.
        /// </summary>
        /// <param name="periapsis">Periapsis distance q. Must be positive.</param>
        /// <returns>Semi Parameter / Semi Latus Rectum p = 2q</returns>
        /// <exception cref="ArgumentException">Thrown when periapsis is not positive</exception>
        public static float CalculateSemiParameterFromPeriapsis(float periapsis) {
            if (periapsis <= 0) throw new ArgumentException("Periapsis must be positive");

            return 2f * periapsis;
        }


        /// <summary>
        /// Calculates the semi-latus rectum (semi-parameter) of an orbit from the specific angular momentum and standard gravitational parameter.
        /// </summary>
        /// <param name="h">The specific angular momentum h, typically in units of m²/s.</param>
        /// <param name="μ">The standard gravitational parameter of the central body.</param>
        /// <returns>The semi-latus rectum p</returns>
        public static float CalculateSemiParameterFromAngularMomentum(float h, float μ = OrbitalParameters.Mu) {
            return h * h / μ;
        }


        /// <summary>
        /// Calculates Semi-Minor Axis
        /// </summary>
        /// <param name="e">Eccentricity</param>
        /// <param name="a">Semi-Major Axis</param>
        /// <returns>Semi-Minor Axis b</returns>
        /// <exception cref="ArgumentException">Thrown if Semi-Major Axis is zero, sign is inconsistent with
        /// eccentricity (a &gt; 0 with e &gt; 1, or a &lt; 0 with e &lt; 1), or eccentricity invalid</exception>
        public static float CalculateSemiMinorAxis(float e, float a) {
            if (MathF.Abs(a) < Program.EPS) throw new ArgumentException("Semi-major axis cannot be zero");
            if (e < 0) throw new ArgumentException("Eccentricity cannot be negative");
            if (GetOrbitType(e) == OrbitType.Parabolic) throw new ArgumentException("Semi-minor axis is undefined for parabolic orbits (e = 1)");
            if (a > 0 && e > 1f) throw new ArgumentException("Semi-major axis must be negative for hyperbolic orbits (e > 1)");
            if (a < 0 && e < 1f) throw new ArgumentException("Semi-major axis must be positive for elliptical orbits (e < 1)");

            float val = (e < 1f) ? (1 - e * e) : (e * e - 1);
            return MathF.Abs(a) * MathF.Sqrt(val);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="ap">Apoapsis</param>
        /// <param name="pe">Periapsis</param>
        /// <returns>Semi-major Axis a</returns>
        /// <exception cref="ArgumentException">Throws if ap/pe is not positive</exception>
        public static float CalculateSemiMajorAxis(float ap, float pe) {
            if (ap <= 0 || pe <= 0) throw new ArgumentException("Distances must be positive");

            return (ap + pe) / 2;
        }


        /// <summary>
        /// Calculates Semi-Major Axis from orbital period using Kepler's Third Law
        /// </summary>
        /// <param name="period">Orbital period Time</param>
        /// <param name="μ">Gravitational parameter</param>
        /// <returns>Semi-major Axis a</returns>
        /// <exception cref="ArgumentException">Throws if μ or period is not positive</exception>
        public static float CalculateSemiMajorAxisFromPeriod(float period, float μ = OrbitalParameters.Mu) {
            if (μ <= 0) throw new ArgumentException("GM must be positive");
            if (period <= 0) throw new ArgumentException("Orbital period must be positive");

            // a = [ (GM * T²) / (4π²) ]^(1/3)
            return MathF.Pow(μ * period * period / (4f * MathF.PI * MathF.PI), 1f / 3f);
        }


        /// <summary>
        /// Calculates Apoapsis
        /// </summary>
        /// <param name="e">Eccentricity</param>
        /// <param name="a">Semi-Major Axis</param>
        /// <returns>Apoapsis Ap/r_ap/r_max</returns>
        /// <exception cref="ArgumentException">Throws error for invalid eccentricity or hyperbolic orbit.</exception>
        public static float CalculateApoapsis(float e, float a) {
            if (e >= 1f - Program.EPS) throw new ArgumentException("Apoapsis is not defined for parabolic and hyperbolic orbits (e >= 1)");

            return a * (1 + e);
        }


        /// <summary>
        /// Calculates Apoapsis
        /// </summary>
        /// <param name="e">Eccentricity</param>
        /// <param name="p">Semi-Parameter</param>
        /// <returns>Apoapsis Ap/r_ap/r_max</returns>
        /// <exception cref="ArgumentException">Throws error for invalid eccentricity or hyperbolic orbit.</exception>
        public static float CalculateApoapsisFromSemiParameter(float e, float p) {
            if (e >= 1f - Program.EPS) throw new ArgumentException("Apoapsis is not defined for parabolic and hyperbolic orbits (e >= 1)");
            if (MathF.Abs(1 - e) < Program.EPS) throw new ArgumentException("Invalid eccentricity");

            return p / (1 - e);
        }

        /// <summary>
        /// Calculates Periapsis
        /// </summary>
        /// <param name="e">Eccentricity</param>
        /// <param name="a">Semi-Major Axis or Semi Parameter</param>
        /// <returns>Periapsis Pe/r_pe/r_min</returns>
        /// <exception cref="ArgumentException">Throws when eccentricity is not positive</exception>
        public static float CalculatePeriapsis(float e, float a) {
            if (e < 0) throw new ArgumentException("Eccentricity cannot be negative");

            return a * (1 - e);
        }


        /// <summary>
        /// Calculates Periapsis
        /// </summary>
        /// <param name="e">Eccentricity</param>
        /// <param name="p">Semi-Parameter</param>
        /// <returns>Periapsis Pe/r_pe/r_min</returns>
        public static float CalculatePeriapsisFromSemiParameter(float e, float p) {
            if (e < 0) throw new ArgumentException("Eccentricity cannot be negative");
            if (MathF.Abs(1 + e) < Program.EPS) throw new ArgumentException("Invalid eccentricity");

            return p / (1 + e);
        }


        /// <summary>
        /// Calculates the orbital period of a body in space using its semi-major axis and the standard gravitational parameter.
        /// </summary>
        /// <param name="a">Semi-Major Axis</param>
        /// <param name="μ">Gravitational parameter</param>
        /// <returns>The orbital period T, in seconds</returns>
        /// <exception cref="ArgumentException">Thrown if a or GM <= 0</exception>
        public static float CalculateOrbitalPeriod(float a, float μ = OrbitalParameters.Mu) {
            if (a <= 0 || μ <= 0) throw new ArgumentException("Orbital period is only defined for elliptical orbits (a > 0)");

            return 2 * MathF.PI * MathF.Sqrt(MathF.Pow(a, 3) / μ);
        }


        /// <summary>
        /// Calculated eccentricity from eccentricity vector
        /// </summary>
        /// <param name="e">Eccentricity vector</param>
        /// <returns>Eccentricity e</returns>
        public static float CalculateEccentricityFromVector(Vector3 e) {
            return e.Length();
        }


        /// <summary>
        /// Calculates eccentricity
        /// </summary>
        /// <param name="ap">Apoapsis</param>
        /// <param name="pe">Periapsis</param>
        /// <returns>Eccentricity e</returns>
        public static float CalculateEccentricityFromApPe(float ap, float pe) {
            return (ap - pe) / (ap + pe);
        }


        /// <summary>
        /// Calculates eccentricity
        /// </summary>
        /// <param name="a">Semi-Major Axis</param>
        /// <param name="b">Semi-Minor Axis</param>
        /// <returns>Eccentricity e</returns>
        public static float CalculateEccentricityFromAxes(float a, float b) {
            return MathF.Sqrt(1 - (b * b) / (a * a));
        }


        /// <summary>
        /// Calculates the orbital eccentricity vector for a two-body system given the position and velocity vectors.
        /// </summary>
        /// <remarks>The eccentricity vector describes the shape and orientation of the orbit. A zero
        /// vector indicates a circular orbit, while the magnitude indicates the orbit's eccentricity.</remarks>
        /// <param name="r">The position vector of the orbiting body relative to the central body, in Cartesian coordinates.</param>
        /// <param name="v">The velocity vector of the orbiting body, in Cartesian coordinates.</param>
        /// <param name="μ">The standard gravitational parameter (μ).</param>
        /// <returns>Orbital eccentricity vector e. The direction points toward periapsis, and the magnitude equals the orbit's eccentricity.</returns>
        public static Vector3 CalculateEccentricityVector(Vector3 r, Vector3 v, float μ = OrbitalParameters.Mu) {
            if (μ <= 0) throw new ArgumentException("GM must be positive");
            float rMag = r.Length();
            if (rMag < Program.EPS) throw new ArgumentException("Position vector too small");

            Vector3 h = CalculateAngularMomentumVector(r, v);
            Vector3 eVec = v.Cross(h) / μ - r.Normalized();
            return eVec;
        }


        /// <summary>
        /// Calculates eccentricity
        /// </summary>
        /// <param name="a">Semi-Major Axis</param>
        /// <param name="c">Linear Eccentricity</param>
        /// <returns>Eccentricity e</returns>
        public static float CalculateEccentricityFromLinear(float a, float c) {
            return c / a;
        }


        /// <summary>
        /// Calculates linear eccentricity
        /// </summary>
        /// <param name="a">Semi-Major Axis</param>
        /// <param name="e">Eccentricity</param>
        /// <returns>Linear eccentricity c</returns>
        /// <exception cref="ArgumentException">Thrown if a not positive</exception>
        public static float CalculateLinearEccentricity(float a, float e) {
            if (MathF.Abs(a) < Program.EPS) throw new ArgumentException("Semi-major axis cannot be zero");

            return a * e;
        }


        /// <summary>
        /// Calculates linear eccentricity
        /// </summary>
        /// <param name="a">Semi-Major Axis</param>
        /// <param name="b">Semi-Minor Axis</param>
        /// <returns>Linear eccentricity c</returns>
        /// <exception cref="ArgumentException">Thrown if a not positive</exception>
        public static float CalculateLinearEccentricityUsingSemiMinorAxis(float a, float b) {
            if (MathF.Abs(a) < Program.EPS) throw new ArgumentException("Semi-major axis cannot be zero");
            float val = a * a - b * b;
            if (val < -Program.EPS) throw new ArgumentException("Invalid axes");

            return MathF.Sqrt(MathF.Max(0, val));
        }


        /// <summary>
        /// Calculates distance from central body in Focus to the sattelite.
        /// </summary>
        /// <param name="e">Eccentricity</param>
        /// <param name="p">Semi Perimeter</param>
        /// <param name="v">Nu/True Anomaly</param>
        /// <returns>Distance from Satttelite r</returns>
        /// <exception cref="ArgumentException"></exception>
        public static float CalculateDistanceToSatellite(float e, float p, float v) {
            float denom = 1 + e * MathF.Cos(v);
            return p / denom;
        }


        /// <summary>
        /// Calculates specific orbital energy
        /// </summary>
        /// <param name="a">Semi-Major Axis</param>
        /// <param name="μ">Gravitational parameter</param>
        /// <returns>Specific orbital energy e/Ɛ</returns>
        /// <exception cref="ArgumentException">Thrown if a not positive</exception>
        public static float CalculateSpecificOrbitalEnergy(float a, float μ = OrbitalParameters.Mu) {
            if (MathF.Abs(a) < Program.EPS) throw new ArgumentException("Semi-major axis cannot be zero");

            return -(μ / (2 * a));
        }


        /// <summary>
        /// Calculates specific orbital energy
        /// </summary>
        /// <param name="r">Distance to sattelite</param>
        /// <param name="v">Velocity</param>
        /// <param name="μ">Gravitational parameter</param>
        /// <returns>Specific orbital energy e/Ɛ</returns>
        /// <exception cref="ArgumentException">Throws if radius not positive</exception>
        public static float CalculateSpecificOrbitalEnergy(float r, float v, float μ = OrbitalParameters.Mu) {
            if (r <= 0) throw new ArgumentException("Radius must be positive");

            return (v * v) / 2 - (μ / r);
        }


        /// <summary>
        /// Determines the type of orbit based on eccentricity
        /// </summary>
        /// <param name="e">Eccentricity</param>
        /// <returns>Orbit type enum</returns>
        public static OrbitType GetOrbitType(float e) {
            if (e < 1f - Program.EPS) return OrbitType.Elliptical;
            if (MathF.Abs(e - 1f) < Program.EPS) return OrbitType.Parabolic;
            return OrbitType.Hyperbolic;
        }


        /// <summary>
        /// Calculates mean orbital velocity
        /// </summary>
        /// <param name="μ">Gravitational parameter</param>
        /// <param name="r">Distance</param>
        /// <returns>Mean orbital velocity v aproximation</returns>
        /// <exception cref="ArgumentException">Throws if μ or r is not positive</exception>
        public static float CalculateCircularVelocity(float r, float μ = OrbitalParameters.Mu) {
            if (μ <= 0 || r <= 0) throw new ArgumentException("GM and radius must be positive");

            return MathF.Sqrt(μ / r);
        }


        /// <summary>
        /// Calculates mean orbital velocity
        /// </summary>
        /// <param name="a">Semi-Major Axis</param>
        /// <param name="t">Orbital period time</param>
        /// <returns>Mean orbital velocity v aproximation</returns>
        /// <exception cref="ArgumentException">Throws if a or t is not positive</exception>
        public static float CalculateCircularVelocityFromPeriod(float a, float t) {
            if (a <= 0 || t <= 0) throw new ArgumentException("Semi-major axis and period must be positive");

            return 2f * MathF.PI * a / t;
        }


        /// <summary>
        /// Calculates the mean motion for an elliptical orbit given the semi-major axis and the standard gravitational parameter
        /// </summary>
        /// <param name="a">The semi-major axis of the orbit, in meters. Must be greater than zero.</param>
        /// <param name="μ">The standard gravitational parameter (μ), in m³/s². Defaults to the value defined by OrbitalParameters.mu if
        /// not specified.</param>
        /// <returns>The mean motion of the orbit, in radians per second.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="a"/> is less than or equal to zero.</exception>
        public static float CalculateMeanMotion(float a, float μ = OrbitalParameters.Mu) {
            if (a <= 0) throw new ArgumentException("Mean motion only defined for elliptical orbits (a > 0)");

            return MathF.Sqrt(μ / (a * a * a));
        }


        /// <summary>
        /// Calculates instantaneous orbital speed at any point on the orbit.
        /// </summary>
        /// <param name="a">Semi-major axis (can be negative for hyperbolas)</param>
        /// <param name="r">Current radial distance</param>
        /// <param name="μ">Gravitational parameter μ</param>
        /// <returns>Returns orbital speed v</returns>
        /// <exception cref="ArgumentException">Throws if radius or GM is not positive</exception>
        public static float CalculateOrbitalVelocity(float a, float r, float μ = OrbitalParameters.Mu) {
            if (μ <= 0) throw new ArgumentException("GM must be positive");
            if (r <= 0) throw new ArgumentException("Radius must be positive");

            return MathF.Sqrt(μ * (2f / r - 1f / a));
        }


        /// <summary>
        /// Calculates the true anomaly of an orbiting body given its position and velocity vectors, eccentricity, and Semi-latus rectum.
        /// </summary>
        /// <remarks>The true anomaly describes the angle between the direction of periapsis and the
        /// current position of the body, as seen from the central focus of the orbit. This method assumes the input
        /// vectors are expressed in a consistent coordinate system.</remarks>
        /// <param name="r">The position vector of the orbiting body, in the reference frame of the central body.</param>
        /// <param name="v">The velocity vector of the orbiting body, in the reference frame of the central body.</param>
        /// <param name="e">The orbital eccentricity. Must be greater than or equal to 0.</param>
        /// <param name="p">The semi-latus rectum of the orbit, typically in the same units as the position vector. Must be greater than 0.</param>
        /// <returns>The true anomaly, in radians, corresponding to the specified orbital state.</returns>
        public static float CalculateTrueAnomaly(Vector3 r, Vector3 v, float e, float p) {
            float rMag = r.Length();
            float vr = r.Dot(v) / rMag;
            return CalculateTrueAnomalyFromRadius(e, p, rMag, vr);
        }


        /// <summary>
        /// Calculates true anomaly (Nu/ν) from radius, eccentricity, and auxiliary velocity info.
        /// Works for elliptical and hyperbolic orbits.
        /// </summary>
        /// <param name="e">Eccentricity</param>
        /// <param name="p">Semi-latus rectum</param>
        /// <param name="r">Current radius</param>
        /// <param name="vr">
        /// Radial velocity (dot(r, v) / |r|). Required to resolve quadrant ambiguity.
        /// Positive = moving away from periapsis, Negative = moving toward periapsis.
        /// </param>
        /// <returns>True anomaly ν in range [0, 2π)</returns>
        /// <exception cref="ArgumentException"></exception>
        public static float CalculateTrueAnomalyFromRadius(float e, float p, float r, float vr) {
            if (r <= 0) throw new ArgumentException("Radius must be positive");

            if (MathF.Abs(e) < Program.EPS) return 0f; // circular orbit (ν undefined -> return 0 convention)

            // Compute cos(ν)
            float cosNu = (p / r - 1f) / e;
            cosNu = Math.Clamp(cosNu, -1f, 1f);

            // Compute sin(ν) using radial velocity sign
            float sinNu = MathF.Sqrt(MathF.Max(0f, 1f - cosNu * cosNu));

            // If radial velocity is negative → descending branch
            if (vr < 0) sinNu = -sinNu;

            // Use atan2 for full quadrant correctness
            float ν = MathF.Atan2(sinNu, cosNu);

            // Normalize to [0, 2π)
            if (ν < 0) ν += 2f * MathF.PI;

            return ν;
        }



        public static float CalculateMeanAnomalyForHyperbolic(float e, float nu) {
            float factor = MathF.Sqrt((e - 1) / (e + 1));
            float tanHalfNu = MathF.Tan(nu / 2f);
            float H = 2f * MathF.Atanh(factor * tanHalfNu);

            return e * MathF.Sinh(H) - H;
        }



        /// <summary>
        /// Calculates true anomaly from eccentric anomaly (for elliptical orbits).
        /// </summary>
        /// <param name="e">Eccentricity</param>
        /// <param name="E">Eccentric Anomaly</param>
        /// <returns>True anomaly Nu/ν</returns>
        /// <exception cref="ArgumentException">Throws if e >= 1</exception>
        public static float CalculateTrueAnomalyFromEccentricAnomaly(float e, float E) {
            if (e >= 1f) throw new ArgumentException("This formula is for elliptical orbits only");

            float tanHalfNu = MathF.Sqrt((1 + e) / (1 - e)) * MathF.Tan(E / 2f);
            return 2f * MathF.Atan(tanHalfNu);
        }


        /// <summary>
        /// Specific angular momentum h = sqrt(μ * p).
        /// </summary>
        /// <param name="p">Semi parameter</param>
        /// <param name="μ">Gravitational parameter μ</param>
        /// <returns>Specific angular momentum h</returns>
        /// <exception cref="ArgumentException">Throws if μ or p is not positive</exception>
        public static float CalculateSpecificAngularMomentum(float p, float μ = OrbitalParameters.Mu) {
            if (μ <= 0 || p <= 0) throw new ArgumentException("GM and p must be positive");

            return MathF.Sqrt(μ * p);
        }


        /// <summary>
        /// Flight path angle φ (angle between velocity and local horizontal).
        /// </summary>
        /// <param name="e">Eccentricity</param>
        /// <param name="v">True enomaly Nu/ν</param>
        /// <returns>Flight path angle φ<returns>
        public static float CalculateFlightPathAngle(float e, float v) {
            return MathF.Atan2(e * MathF.Sin(v), 1 + e * MathF.Cos(v));
        }



        /// <summary>
        /// Calculates the flight path angle φ from position and velocity vectors.
        /// </summary>
        public static float CalculateFlightPathAngle(Vector3 r, Vector3 v) {
            float rMag = r.Length();
            float vMag = v.Length();
            if (rMag < Program.EPS || vMag < Program.EPS) throw new ArgumentException("Invalid vectors");

            // radial velocity
            float vr = r.Dot(v) / rMag;

            // tangential component
            float vtheta = MathF.Sqrt(MathF.Max(0f, vMag * vMag - vr * vr));

            // φ = atan2(vr, vθ)
            return MathF.Atan2(vr, vtheta);
        }


        /// <summary>
        /// Calculates inclination (i) from angular momentum vector
        /// </summary>
        /// <param name="h">Angular momentum vector</param>
        /// <returns>Inclination i</returns>
        public static float CalculateInclination(Vector3 h) {
            float hz = h.Z;
            float hMag = h.Length();
            if (hMag < Program.EPS) throw new ArgumentException("Angular momentum vector is too small");

            return MathF.Acos(Math.Clamp(hz / hMag, -1f, 1f));
        }


        /// <summary>
        /// Calculates RAAN (Ω) - Right Ascension of Ascending Node
        /// </summary>
        /// <param name="h">Angular momentum vector</param>
        /// <returns>Right Ascension of Ascending Node Ω</returns>
        public static float CalculateRAAN(Vector3 h) {
            Vector3 k = new(0, 0, 1);
            Vector3 node = k.Cross(h);
            return CalculateRAANFromNode(node);
        }


        /// <summary>
        /// Calculates RAAN from Node vector
        /// </summary>
        /// <param name="node">Node vector from k x h</param>
        /// <returns></returns>
        private static float CalculateRAANFromNode(Vector3 node) {
            float nodeMag = node.Length();
            if (nodeMag < Program.EPS) return 0f;

            float Ω = MathF.Acos(Math.Clamp(node.X / nodeMag, -1f, 1f));

            if (node.Y < 0) Ω = 2f * MathF.PI - Ω;

            return NormalizeAngle(Ω);
        }



        /// <summary>
        /// Calculates Argument of Periapsis (ω)
        /// </summary>
        /// <remarks>The argument of periapsis describes the orientation of the orbit's closest approach
        /// relative to the ascending node. The returned value is in the range [0, 2π].</remarks>
        /// <param name="h">The specific angular momentum vector of the orbit.</param>
        /// <param name="e">The eccentricity vector of the orbit.</param>
        /// <returns>The argument of periapsis ω, in radians, measured from the ascending node to the periapsis direction within
        /// the orbital plane.</returns>
        public static float CalculateArgumentOfPeriapsis(Vector3 h, Vector3 e) {
            Vector3 node = new Vector3(0, 0, 1).Cross(h);
            float nodeMag = node.Length();
            float eMag = e.Length();

            if (nodeMag < Program.EPS || eMag < Program.EPS)
                return 0f;

            float cosOmega = node.Dot(e) / (nodeMag * eMag);
            float ω = MathF.Acos(Math.Clamp(cosOmega, -1f, 1f));

            if (node.Cross(e).Dot(h) < 0) ω = 2f * MathF.PI - ω;

            return NormalizeAngle(ω);
        }


        /// <summary>
        /// Calculates the angular momentum vector as the cross product of the position and velocity vectors.
        /// </summary>
        /// <param name="r">The position vector of the orbiting body relative to the central body, in Cartesian coordinates.</param>
        /// <param name="v">The velocity vector of the orbiting body, in Cartesian coordinates.</param>
        /// <returns>A Vector3 representing the angular momentum vector.</returns>
        public static Vector3 CalculateAngularMomentumVector(Vector3 r, Vector3 v) {
            return r.Cross(v);
        }


        public static OrbitalParameters CalculateOrbitalElementsFromState(Vector3 F, Vector3 r, Vector3 v, float mu = OrbitalParameters.Mu) {
            if (mu <= 0) throw new ArgumentException("GM must be positive");

            float rMag = r.Length();
            float vMag = v.Length();
            if (rMag < Program.EPS) throw new ArgumentException("Position vector is near zero");

            Vector3 h = CalculateAngularMomentumVector(r, v);
            float hMag = h.Length();

            if (hMag < Program.EPS) throw new ArgumentException("Angular momentum too small");

            Vector3 eVec = CalculateEccentricityVector(r, v, mu);
            float e = CalculateEccentricityFromVector(eVec);

            float energy = CalculateSpecificOrbitalEnergy(rMag, vMag, mu);
            float a = -mu / (2f * energy);

            float p = CalculateSemiParameterFromAngularMomentum(hMag, mu);
            float i = CalculateInclination(h);

            Vector3 node = new Vector3(0, 0, 1).Cross(h);
            float nodeMag = node.Length();

            float Ω = CalculateRAANFromNode(node);

            float ω = 0f;
            if (nodeMag > Program.EPS && e > Program.EPS) ω = CalculateArgumentOfPeriapsis(h, eVec);

            float nu = (e > Program.EPS) ? CalculateTrueAnomaly(r, v, e, p) : 0f;
            nu = NormalizeAngle(nu);

            // --- Construct object ---
            OrbitalParameters parameters = new(F, a, e, nu, i, Ω, ω);

            // --- Derived anomalies (elliptical only) ---
            if (e < 1f - Program.EPS) {
                parameters.EccentricAnomaly = CalculateEccentricAnomaly(e, nu);
                parameters.MeanAnomaly = CalculateMeanAnomaly(parameters.EccentricAnomaly, e);
            } else {
                parameters.EccentricAnomaly = 0f;
                parameters.MeanAnomaly = 0f;
            }

            return parameters;
        }


        /// <summary>
        /// Computes position and velocity vectors in inertial frame from orbital elements.
        /// Uses perifocal frame then applies rotations for ω, i, Ω.
        /// </summary>
        /// <param name="orbit">Six basic orbital parameters to calculate from</param>
        /// <param name="μ">Gravitational parameter</param>
        /// <returns>Vectors r and v, which magnitudes gives us orbital height and velocity.</returns>
        public static (Vector3 r, Vector3 v) CalculateOrbitalVectorsFromParameters(OrbitalParameters op, float μ = OrbitalParameters.Mu) {
            float p = op.SemiParameter;
            float e = op.Eccentricity;
            float ν = NormalizeAngle(op.TrueAnomaly);
            float ω = NormalizeAngle(op.ArgumentOfPeriapsis);
            float i = op.Inclination;
            float Ω = NormalizeAngle(op.RightAscensionOfAscendingNode);

            // --- Radius ---
            float rMag = CalculateDistanceToSatellite(e, p, ν);

            float cosNu = MathF.Cos(ν);
            float sinNu = MathF.Sin(ν);

            // --- PQW position ---
            Vector3 rPQW = new(rMag * cosNu, rMag * sinNu, 0f);

            // --- Angular momentum ---
            float h = CalculateSpecificAngularMomentum(p, μ);

            // --- Velocity components ---
            float vr = (μ / h) * e * sinNu;
            float vtheta = (μ / h) * (1 + e * cosNu);

            Vector3 vPQW = new(
                vr * cosNu - vtheta * sinNu,
                vr * sinNu + vtheta * cosNu,
                0f
            );

            // --- Transform to ECI ---
            Vector3 rECI = RotatePQWToECI(rPQW, ω, i, Ω);
            Vector3 vECI = RotatePQWToECI(vPQW, ω, i, Ω);

            return (rECI, vECI);
        }



        /// <summary>
        /// Calculates the mean anomaly from the eccentric anomaly and orbital eccentricity.
        /// </summary>
        /// <param name="E">The eccentric anomaly, in radians.</param>
        /// <param name="e">The orbital eccentricity. Must be in the range [0, 1).</param>
        /// <returns>The mean anomaly, in radians.</returns>
        public static float CalculateMeanAnomaly(float E, float e) {
            return E - (e * MathF.Sin(E));
        }


        /// <summary>
        /// Calculates eccentric anomaly from true anomaly (for elliptical orbits).
        /// </summary>
        /// <param name="e">Eccentricity</param>
        /// <param name="ν">True anomaly</param>
        /// <returns>Eccentric Anomaly E</returns>
        /// <exception cref="ArgumentException">Throws if e >= 1</exception>
        public static float CalculateEccentricAnomaly(float e, float ν) {
            if (e >= 1f) return float.NaN; //This formula is for elliptical orbits only
            float E = MathF.Atan2(MathF.Sqrt(1 - e * e) * MathF.Sin(ν), e + MathF.Cos(ν));
            return NormalizeAngle(E);
        }


        /// <summary>
        /// Calculates the parabolic anomaly D from true anomaly (for parabolic orbits, e = 1).
        /// </summary>
        /// <remarks>D = tan(ν/2). Unlike elliptical/hyperbolic anomalies this is not an angle but a
        /// unitless parameter used directly in Barker's equation.</remarks>
        /// <param name="ν">True anomaly, in radians.</param>
        /// <returns>Parabolic anomaly D.</returns>
        public static float CalculateParabolicAnomaly(float ν) {
            return MathF.Tan(ν / 2f);
        }


        /// <summary>
        /// Calculates the mean anomaly of a parabolic orbit from the parabolic anomaly using Barker's equation.
        /// </summary>
        /// <param name="D">Parabolic anomaly (D = tan(ν/2)).</param>
        /// <returns>Mean anomaly M = D + D³/3.</returns>
        public static float CalculateMeanAnomalyParabolic(float D) {
            return D + (D * D * D) / 3f;
        }


        #region Helpers


        /// <summary>
        /// Normalizes angle
        /// </summary>
        /// <param name="angle">Angle size</param>
        /// <returns></returns>
        public static float NormalizeAngle(float angle) {
            float twoPi = 2f * MathF.PI;
            angle %= twoPi;
            if (angle < 0) angle += twoPi;
            return angle;
        }


        /// <summary>
        /// Transforms a vector from the perifocal (PQW) coordinate system to the Earth-Centered Inertial (ECI) coordinate system using the specified orbital elements.
        /// </summary>
        /// <remarks>The transformation applies rotations in the order of argument of periapsis,
        /// inclination, and right ascension of the ascending node. All angles must be specified in radians.</remarks>
        /// <param name="vec">The vector in the perifocal (PQW) coordinate system to be transformed.</param>
        /// <param name="ω">The argument of periapsis, in radians. Specifies the rotation about the perifocal Z-axis.</param>
        /// <param name="i">The inclination, in radians. Specifies the rotation about the X-axis.</param>
        /// <param name="Ω">The right ascension of the ascending node, in radians. Specifies the rotation about the inertial Z-axis.</param>
        /// <returns>A Vector3 representing the input vector transformed to the ECI coordinate system.</returns>
        private static Vector3 RotatePQWToECI(Vector3 vec, float ω, float i, float Ω) {
            // Apply rotations in reverse order: first -ω, then -i, then -Ω
            vec = RotateZ(vec, ω);
            vec = RotateX(vec, i);
            vec = RotateZ(vec, Ω);
            return vec;
        }


        private static Vector3 RotateZ(Vector3 v, float angle) {
            float c = MathF.Cos(angle), s = MathF.Sin(angle);
            return new Vector3(
                (c * v.X - s * v.Y),
                (s * v.X + c * v.Y),
                v.Z
            );
        }


        private static Vector3 RotateX(Vector3 v, float angle) {
            float c = MathF.Cos(angle), s = MathF.Sin(angle);
            return new Vector3(
                v.X,
                (c * v.Y - s * v.Z),
                (s * v.Y + c * v.Z)
            );
        }


        public static float ToDegrees(float radians) => radians * 180f / MathF.PI;


        public static float ToRadians(float degrees) => degrees * MathF.PI / 180f;
        #endregion
    }
}
