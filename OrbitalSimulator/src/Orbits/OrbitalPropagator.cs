using Godot;
using System;
using System.Collections.Generic;

namespace OrbitalSimulator.src.Orbits {
    public static class OrbitalPropagator {

        /// <summary>
        /// Solves Kepler's equation for elliptical orbits and returns the eccentric anomaly corresponding to the specified mean anomaly and eccentricity.
        /// Solves Kepler's Equation: M = E - e*sin(E)
        /// Only valid for elliptical orbits (e < 1).
        /// </summary>
        /// <remarks>The result is normalized to the range [0, 2π).</remarks>
        /// <param name="M">The mean anomaly, in radians.</param>
        /// <param name="e">The orbital eccentricity.</param>
        /// <param name="maxIterations">The maximum number of iterations when solving. Must be positive. The default is 10.</param>
        /// <param name="tolerance">The convergence tolerance for the solution. Iteration stops when the change is less than this value. Default is 1e-6.</param>
        /// <returns>The eccentric anomaly, in radians.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="e"/> is greater than or equal to 1, indicating a non-elliptical orbit.</exception>
        public static float SolveKepler(float M, float e, int maxIterations = 10, float tolerance = 1e-6f) {
            if (e >= 1f) throw new ArgumentException("SolveKepler only supports elliptical orbits (e < 1)");

            // Initial guess
            float E = M;

            for (int i = 0; i < maxIterations; i++) {
                float f = E - e * MathF.Sin(E) - M;
                float fPrime = 1 - e * MathF.Cos(E);

                float delta = f / fPrime;
                E -= delta;

                if (MathF.Abs(delta) < tolerance) break;
            }

            return OrbitalMath.NormalizeAngle(E);
        }


        /// <summary>
        /// Propagates the position and velocity vectors of an orbiting body forward by a specified time interval, assuming an elliptical orbit.
        /// Propagates orbit forward by time t (seconds). Returns updated position and velocity in ECI frame.
        /// </summary>
        /// <param name="op">The orbital parameters describing the initial state of the orbiting body.</param>
        /// <param name="t">The time interval, in seconds, by which to propagate the orbit from the initial state.</param>
        /// <param name="μ">The standard gravitational parameter (μ)</param>
        /// <returns>A tuple containing the propagated position vector (r) and velocity vector (v) in the inertial reference frame at the specified time.</returns>
        /// <exception cref="ArgumentException">Thrown if non-elliptical orbit</exception>
        public static (Vector3 r, Vector3 v) Propagate(OrbitalParameters op, float t, float μ = OrbitalParameters.Mu) {
            float a = op.SemiMajorAxis;
            float e = op.Eccentricity;

            float nu;

            if (OrbitalMath.GetOrbitType(e) == OrbitType.Parabolic) {
                // Parabolic mean motion is defined via the semi-parameter, since a is undefined (infinite) for e = 1.
                float p = op.SemiParameter;
                float nPar = MathF.Sqrt(μ / (2f * p * p * p));

                float M = op.MeanAnomaly + nPar * t;

                float D = SolveBarkerEquation(M);
                nu = ParabolicToTrueAnomaly(D);
            } else {
                // --- Mean motion ---
                float n = MathF.Sqrt(μ / MathF.Abs(a * a * a));

                if (e < 1f) {
                    float M = OrbitalMath.NormalizeAngle(op.MeanAnomaly + n * t);

                    float E = SolveKepler(M, e);
                    nu = OrbitalMath.CalculateTrueAnomalyFromEccentricAnomaly(e, E);
                } else {
                    float M0 = OrbitalMath.CalculateMeanAnomalyForHyperbolic(e, op.TrueAnomaly);
                    float M = M0 + n * t;

                    float H = SolveKeplerHyperbolic(M, e);
                    nu = HyperbolicToTrueAnomaly(e, H);
                }
            }

            var updated = new OrbitalParameters(op.Focus, a, e, nu, op.Inclination, op.RightAscensionOfAscendingNode, op.ArgumentOfPeriapsis);

            return OrbitalMath.CalculateOrbitalVectorsFromParameters(updated, μ);
        }

        public static float SolveKeplerHyperbolic(float M, float e, int maxIterations = 15, float tolerance = 1e-6f) {
            if (e <= 1f) throw new ArgumentException("Hyperbolic solver requires e > 1");

            // Initial guess (good heuristic)
            float H = MathF.Log(2 * MathF.Abs(M) / e + 1.8f);

            for (int i = 0; i < maxIterations; i++) {
                float sinhH = MathF.Sinh(H);
                float coshH = MathF.Cosh(H);

                float f = e * sinhH - H - M;
                float fPrime = e * coshH - 1;

                float delta = f / fPrime;
                H -= delta;

                if (MathF.Abs(delta) < tolerance)
                    break;
            }

            return H;
        }

        public static float HyperbolicToTrueAnomaly(float e, float H) {
            float tanhHalfH = MathF.Tanh(H / 2f);

            float factor = MathF.Sqrt((e + 1) / (e - 1));

            float tanHalfNu = factor * tanhHalfH;

            return 2f * MathF.Atan(tanHalfNu);
        }


        /// <summary>
        /// Solves Barker's equation for parabolic orbits and returns the parabolic anomaly D corresponding to the
        /// specified mean anomaly.
        /// Solves Barker's Equation: M = D + D³/3
        /// Only valid for parabolic orbits (e = 1).
        /// </summary>
        /// <param name="M">The mean anomaly.</param>
        /// <param name="maxIterations">The maximum number of iterations when solving. Must be positive. The default is 15.</param>
        /// <param name="tolerance">The convergence tolerance for the solution. Iteration stops when the change is less than this value. Default is 1e-6.</param>
        /// <returns>The parabolic anomaly D.</returns>
        public static float SolveBarkerEquation(float M, int maxIterations = 15, float tolerance = 1e-6f) {
            // Initial guess. For small M, D ≈ M works well; Newton's method converges quickly regardless.
            float D = M;

            for (int i = 0; i < maxIterations; i++) {
                float f = OrbitalMath.CalculateMeanAnomalyParabolic(D) - M;
                float fPrime = 1f + D * D;

                float delta = f / fPrime;
                D -= delta;

                if (MathF.Abs(delta) < tolerance) break;
            }

            return D;
        }


        /// <summary>
        /// Converts a parabolic anomaly D to true anomaly ν (for parabolic orbits, e = 1).
        /// </summary>
        /// <param name="D">Parabolic anomaly (D = tan(ν/2)).</param>
        /// <returns>True anomaly ν, in radians.</returns>
        public static float ParabolicToTrueAnomaly(float D) {
            return 2f * MathF.Atan(D);
        }


        public static List<Vector3> GenerateEllipsePoints(OrbitalParameters op, int segments = 200) {
            var points = new List<Vector3>();

            for (float i = 0; i <= segments; i++) {
                float t = i / segments;
                float nu = t * 2f * MathF.PI;

                var temp = new OrbitalParameters(op.Focus, op.SemiMajorAxis, op.Eccentricity, nu, op.Inclination, op.RightAscensionOfAscendingNode, op.ArgumentOfPeriapsis);

                var (r, _) = OrbitalMath.CalculateOrbitalVectorsFromParameters(temp);
                points.Add(r);
            }

            return points;
        }


        public static List<Vector3> GenerateHyperbolicPoints(OrbitalParameters op, int steps = 150) {
            var points = new List<Vector3>();
            float e = op.Eccentricity;

            if (e <= 1f) return points; // fallback

            // Calculate the maximum true anomaly (asymptote)
            float nuMax = MathF.Acos(-1f / e) * 0.95f; // 95% of asymptote to avoid infinity

            for (int i = 0; i <= steps; i++) {
                float t = (float)i / steps;
                float nu = Mathf.Lerp(-nuMax, nuMax, t);

                var temp = new OrbitalParameters(
                    op.Focus,
                    op.SemiMajorAxis,
                    e,
                    nu,
                    op.Inclination,
                    op.RightAscensionOfAscendingNode,
                    op.ArgumentOfPeriapsis
                );

                try {
                    var (r, _) = OrbitalMath.CalculateOrbitalVectorsFromParameters(temp);
                    if (r.IsFinite()) points.Add(r);
                } catch { }
            }

            return points;
        }


        /// <summary>
        /// Generates a set of points along a parabolic orbit (e = 1) for rendering purposes.
        /// </summary>
        /// <remarks>Since a parabolic trajectory extends to infinity as true anomaly approaches ±π,
        /// the true anomaly range is bounded to a fraction of that limit to keep the generated points finite.</remarks>
        /// <param name="op">The orbital parameters describing the parabolic orbit. SemiMajorAxis is interpreted as the periapsis distance.</param>
        /// <param name="steps">The number of segments to generate along the trajectory.</param>
        /// <returns>A list of position vectors along the parabolic trajectory.</returns>
        public static List<Vector3> GenerateParabolicPoints(OrbitalParameters op, int steps = 150) {
            var points = new List<Vector3>();

            if (OrbitalMath.GetOrbitType(op.Eccentricity) != OrbitType.Parabolic) return points; // fallback

            // True anomaly approaches ±π asymptotically; bound to 95% of that to avoid infinity.
            float nuMax = MathF.PI * 0.95f;

            for (int i = 0; i <= steps; i++) {
                float t = (float)i / steps;
                float nu = Mathf.Lerp(-nuMax, nuMax, t);

                var temp = new OrbitalParameters(
                    op.Focus,
                    op.Periapsis,
                    op.Eccentricity,
                    nu,
                    op.Inclination,
                    op.RightAscensionOfAscendingNode,
                    op.ArgumentOfPeriapsis
                );

                try {
                    var (r, _) = OrbitalMath.CalculateOrbitalVectorsFromParameters(temp);
                    if (r.IsFinite()) points.Add(r);
                } catch { }
            }

            return points;
        }
    }
}
