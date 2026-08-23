using Godot;
using OrbitalSimulator.src.Orbits;

namespace OrbitalSimulator.Tests
{
    public class OrbitalMathConsistencyTests
    {
        [Fact]
        public void FullOrbitalRoundtrip_Elliptical_ShouldBeConsistent()
        {
            float a = 10000f;
            float e = 0.3f;
            float i = MathF.PI / 6;
            float Ω = MathF.PI / 3;
            float ω = MathF.PI / 4;
            float ν = MathF.PI / 3;

            var orbit = new OrbitalParameters(Vector3.Zero, a, e, ν, i, Ω, ω);

            ValidateOrbitRoundtrip(orbit);
        }


        private static float AngleDiff(float a, float b)
        {
            float diff = MathF.Abs(a - b) % (2 * MathF.PI);
            return MathF.Min(diff, 2 * MathF.PI - diff);
        }


        private static void ValidateOrbitRoundtrip(OrbitalParameters orbit)
        {
            var (r, v) = OrbitalMath.CalculateOrbitalVectorsFromParameters(orbit);
            var reconstructed = OrbitalMath.CalculateOrbitalElementsFromState(Vector3.Zero, r, v);

            // --- Scalars ---
            Assert.True(MathF.Abs(orbit.SemiMajorAxis - reconstructed.SemiMajorAxis) < Program.EPS * 200);
            Assert.True(MathF.Abs(orbit.Eccentricity - reconstructed.Eccentricity) < Program.EPS);
            Assert.True(MathF.Abs(orbit.Inclination - reconstructed.Inclination) < Program.EPS);

            // --- RAAN (only if well-defined) ---
            if (MathF.Abs(MathF.Sin(orbit.Inclination)) > Program.EPS)
            {
                Assert.True(AngleDiff(
                    orbit.RightAscensionOfAscendingNode,
                    reconstructed.RightAscensionOfAscendingNode) < Program.EPS);
            }

            // --- Argument of periapsis (skip near circular) ---
            if (orbit.Eccentricity > Program.EPS)
            {
                Assert.True(AngleDiff(
                    orbit.ArgumentOfPeriapsis,
                    reconstructed.ArgumentOfPeriapsis) < Program.EPS);
            }

            // --- True anomaly (via argument of latitude) ---
            if (orbit.Eccentricity > Program.EPS)
            {
                float u1 = orbit.ArgumentOfPeriapsis + orbit.TrueAnomaly;
                float u2 = reconstructed.ArgumentOfPeriapsis + reconstructed.TrueAnomaly;

                Assert.True(AngleDiff(u1, u2) < Program.EPS);
            }

            // --- Vector validation (RELATIVE, always valid) ---
            var (r2, v2) = OrbitalMath.CalculateOrbitalVectorsFromParameters(reconstructed);

            float relR = (r - r2).Length() / MathF.Max(1f, r.Length());
            float relV = (v - v2).Length() / MathF.Max(1f, v.Length());

            Assert.True(relR < Program.EPS);
            Assert.True(relV < Program.EPS);
        }


        [Fact]
        public void FullOrbitalRoundtrip_Hyperbolic_ShouldBeConsistent()
        {
            float a = -8000f;
            float e = 1.4f;

            float i = 0.3f;
            float Ω = 1.0f;
            float ω = 0.8f;
            float ν = 0.5f;

            var orbit = new OrbitalParameters(Vector3.Zero, a, e, ν, i, Ω, ω);

            ValidateOrbitRoundtrip(orbit);
        }


        [Fact]
        public void OrbitalElements_ShouldBeConsistent()
        {
            float a = 10000f;
            float e = 0.3f;

            float p = OrbitalMath.CalculateSemiParameter(e, a);
            float b = OrbitalMath.CalculateSemiMinorAxis(e, a);
            float ap = OrbitalMath.CalculateApoapsis(e, a);
            float pe = OrbitalMath.CalculatePeriapsis(e, a);

            // a should reconstruct
            float a2 = OrbitalMath.CalculateSemiMajorAxis(ap, pe);
            Assert.Equal(a, a2, Program.EPS);

            // eccentricity should reconstruct
            float e2 = OrbitalMath.CalculateEccentricityFromApPe(ap, pe);
            Assert.Equal(e, e2, Program.EPS);

            // p = a(1 - e^2)
            Assert.Equal(a * (1 - e * e), p, Program.EPS);

            // b = a sqrt(1 - e^2)
            Assert.Equal(a * MathF.Sqrt(1 - e * e), b, Program.EPS);
        }


        [Fact]
        public void ApPe_ShouldBeConsistent()
        {
            float ap = 15000f;
            float pe = 5000f;

            float a = OrbitalMath.CalculateSemiMajorAxis(ap, pe);
            float e = OrbitalMath.CalculateEccentricityFromApPe(ap, pe);

            float ap2 = OrbitalMath.CalculateApoapsis(e, a);
            float pe2 = OrbitalMath.CalculatePeriapsis(e, a);

            Assert.Equal(ap, ap2, Program.EPS);
            Assert.Equal(pe, pe2, Program.EPS);
        }


        [Fact]
        public void RadiusFormula_ShouldBeConsistent()
        {
            float a = 10000f;
            float e = 0.4f;

            float p = OrbitalMath.CalculateSemiParameter(e, a);

            float v = 1.0f; // random anomaly

            float r = OrbitalMath.CalculateDistanceToSatellite(e, p, v);

            float expected = p / (1 + e * MathF.Cos(v));

            Assert.Equal(expected, r, Program.EPS);
        }


        [Fact]
        public void OrbitPeriod_ShouldBeConsistent()
        {
            float a = 10000f;

            float period = OrbitalMath.CalculateOrbitalPeriod(a);
            float aRecovered = OrbitalMath.CalculateSemiMajorAxisFromPeriod(period);

            Assert.Equal(a, aRecovered, Program.EPS * 10);
        }


        [Fact]
        public void TrueAnomaly_EccentricAnomaly_ShouldBeConsistent()
        {
            float e = 0.3f;
            float E = MathF.PI / 3; // 60 degrees eccentric anomaly

            float ν = OrbitalMath.CalculateTrueAnomalyFromEccentricAnomaly(e, E);
            float E_recovered = OrbitalMath.CalculateEccentricAnomaly(e, ν);
            Assert.Equal(E, E_recovered, Program.EPS);
        }


        [Fact]
        public void MeanAnomaly_ShouldBeConsistent()
        {
            float e = 0.4f;
            float M = MathF.PI / 4; // mean anomaly

            // For simplicity we test the relation for small e (or you can implement Kepler solver later)
            float E = M + e * MathF.Sin(M); // rough approximation
            float ν = OrbitalMath.CalculateTrueAnomalyFromEccentricAnomaly(e, E);
            float E_back = OrbitalMath.CalculateEccentricAnomaly(e, ν);

            Assert.True(MathF.Abs(E - E_back) < Program.EPS);
        }


        [Fact]
        public void Constructor_WithSixParameters_ShouldCorrectlyCalculateAllDerivedValues()
        {
            // Arrange - Realistic elliptical orbit (similar to your failing roundtrip test)
            Vector3 centralBody = Vector3.Zero; // Earth-centered
            float semiMajorAxis = 8000f;           // km
            float eccentricity = 0.1f;
            float trueAnomaly = MathF.PI / 3;      // 60°
            float inclination = MathF.PI / 6;      // 30°
            float Ω = MathF.PI / 4;             // 45°
            float argOfPeriapsis = MathF.PI / 3;   // 60°

            // Act
            OrbitalParameters orbit = new(centralBody, semiMajorAxis, eccentricity, trueAnomaly, inclination, Ω, argOfPeriapsis);

            // Assert
            Assert.Equal(OrbitType.Elliptical, orbit.Type);

            // Geometric elements
            float expectedSemiParameter = semiMajorAxis * (1 - eccentricity * eccentricity);
            Assert.Equal(expectedSemiParameter, orbit.SemiParameter, Program.EPS);

            float expectedSemiMinorAxis = semiMajorAxis * MathF.Sqrt(1 - eccentricity * eccentricity);
            Assert.Equal(expectedSemiMinorAxis, orbit.SemiMinorAxis, Program.EPS);

            float expectedPeriapsis = semiMajorAxis * (1 - eccentricity);
            Assert.Equal(expectedPeriapsis, orbit.Periapsis, Program.EPS);

            float expectedApoapsis = semiMajorAxis * (1 + eccentricity);
            Assert.Equal(expectedApoapsis, orbit.Apoapsis, Program.EPS);

            // Orbital period (using mu from the class)
            float expectedPeriod = 2 * MathF.PI * MathF.Sqrt(MathF.Pow(semiMajorAxis, 3) / OrbitalParameters.Mu);

            Assert.Equal(expectedPeriod, orbit.OrbitalPeriod, 1); // 1 second tolerance is fine

            // Input angles should be stored as-is
            Assert.Equal(inclination, orbit.Inclination, Program.EPS);
            Assert.Equal(Ω, orbit.RightAscensionOfAscendingNode, Program.EPS);
            Assert.Equal(argOfPeriapsis, orbit.ArgumentOfPeriapsis, Program.EPS);
            Assert.Equal(trueAnomaly, orbit.TrueAnomaly, Program.EPS);
        }

        [Fact]
        public void Constructor_HyperbolicOrbit_ShouldSetApoapsisToNaN()
        {
            var orbit = new OrbitalParameters(Vector3.Zero, 8000f, 1.2f, 0.5f, 0.3f, 0f, 0f);

            Assert.Equal(OrbitType.Hyperbolic, orbit.Type);
            Assert.True(float.IsNaN(orbit.Apoapsis));
            Assert.True(float.IsNaN(orbit.OrbitalPeriod));
        }


        [Fact]
        public void FullOrbitalRoundtrip_Hyperbolic_Randomised()
        {
            var rnd = new Random(123);

            for (int k = 0; k < 200; k++)
            {

                float a = -5000f - (float)rnd.NextDouble() * 20000f;
                float e = 1.1f + (float)rnd.NextDouble();

                float i = (float)rnd.NextDouble() * MathF.PI;
                float Ω = (float)rnd.NextDouble() * 2 * MathF.PI;
                float ω = (float)rnd.NextDouble() * 2 * MathF.PI;

                float ν = ((float)rnd.NextDouble() - 0.5f) * 2f;

                var orbit = new OrbitalParameters(Vector3.Zero, a, e, ν, i, Ω, ω);

                ValidateOrbitRoundtrip(orbit);
            }
        }

        [Fact]
        public void FullOrbitalRoundtrip_Elliptical_Randomised()
        {
            var rnd = new Random(42);

            for (int k = 0; k < 500; k++)
            {

                float a = 7000f + (float)rnd.NextDouble() * 20000f;

                // Avoid near-circular degeneracy
                float e = 0.01f + (float)rnd.NextDouble() * 0.79f;   // [0.01, 0.8]

                // Avoid near-equatorial degeneracy
                float i = 0.01f + (float)rnd.NextDouble() * (MathF.PI - 0.02f);

                float Ω = (float)rnd.NextDouble() * 2f * MathF.PI;
                float ω = (float)rnd.NextDouble() * 2f * MathF.PI;
                float ν = (float)rnd.NextDouble() * 2f * MathF.PI;

                var orbit = new OrbitalParameters(Vector3.Zero, a, e, ν, i, Ω, ω);

                // reuse your existing validated logic
                ValidateOrbitRoundtrip(orbit);
            }
        }


        [Fact]
        public void Eccentricity_AllMethods_ShouldMatch()
        {
            float a = 10000f;
            float e = 0.4f;

            float ap = OrbitalMath.CalculateApoapsis(e, a);
            float pe = OrbitalMath.CalculatePeriapsis(e, a);

            float b = OrbitalMath.CalculateSemiMinorAxis(e, a);

            float e1 = OrbitalMath.CalculateEccentricityFromApPe(ap, pe);
            float e2 = OrbitalMath.CalculateEccentricityFromAxes(a, b);

            Assert.Equal(e, e1, Program.EPS);
            Assert.Equal(e, e2, Program.EPS);
        }


        [Fact]
        public void Distance_ShouldMatch_PeriapsisAndApoapsisFunctions()
        {
            float a = 10000f;
            float e = 0.3f;

            float p = OrbitalMath.CalculateSemiParameter(e, a);

            float rPe1 = OrbitalMath.CalculateDistanceToSatellite(e, p, 0);
            float rPe2 = OrbitalMath.CalculatePeriapsis(e, a);

            float rAp1 = OrbitalMath.CalculateDistanceToSatellite(e, p, MathF.PI);
            float rAp2 = OrbitalMath.CalculateApoapsis(e, a);

            Assert.Equal(rPe1, rPe2, Program.EPS);
            Assert.Equal(rAp1, rAp2, Program.EPS);
        }


        [Fact]
        public void Energy_ShouldBeConstant_AlongOrbit()
        {
            float a = 10000f;
            float e = 0.5f;

            float p = OrbitalMath.CalculateSemiParameter(e, a);

            float nu1 = 0.3f;
            float nu2 = 2.0f;

            float r1 = OrbitalMath.CalculateDistanceToSatellite(e, p, nu1);
            float r2 = OrbitalMath.CalculateDistanceToSatellite(e, p, nu2);

            float v1 = OrbitalMath.CalculateOrbitalVelocity(a, r1);
            float v2 = OrbitalMath.CalculateOrbitalVelocity(a, r2);

            float E1 = OrbitalMath.CalculateSpecificOrbitalEnergy(r1, v1);
            float E2 = OrbitalMath.CalculateSpecificOrbitalEnergy(r2, v2);

            Assert.Equal(E1, E2, Program.EPS);
        }
    }
}
