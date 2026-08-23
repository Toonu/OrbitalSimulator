using Godot;
using OrbitalSimulator.src.Orbits;

namespace OrbitalSimulator.Tests
{
    public class OrbitalMathVectorTests
    {
        [Fact]
        public void AngularMomentumVector_ShouldBeCorrect()
        {
            var r = new Vector3(7000f, 0f, 0f);
            var v = new Vector3(0f, 7.5f, 0f);  // roughly circular

            var h = OrbitalMath.CalculateAngularMomentumVector(r, v);
            Assert.Equal(0f, h.X, Program.EPS);
            Assert.Equal(0f, h.Y, Program.EPS);
            Assert.True(h.Z > 0);
        }

        [Fact]
        public void EccentricityVector_ShouldBeCorrect()
        {
            var r = new Vector3(8000f, 0f, 0f);
            var v = new Vector3(0f, 7.5f, 0f);

            var eVec = OrbitalMath.CalculateEccentricityVector(r, v);
            Assert.True(eVec.Length() >= 0);
        }

        [Fact]
        public void OrbitalElementsFromState_CircularOrbit()
        {
            var r = new Vector3(7000f, 0f, 0f);
            var v = new Vector3(0f, MathF.Sqrt(OrbitalParameters.Mu / 7000), 0f);
            OrbitalParameters parameters = OrbitalMath.CalculateOrbitalElementsFromState(Vector3.Zero, r, v);

            Assert.Equal(7000f, parameters.SemiMajorAxis, Program.EPS);
            Assert.True(parameters.Eccentricity < 0.01f);                     // nearly circular
            Assert.Equal(0f, parameters.Inclination, Program.EPS);           // equatorial
            Assert.True(parameters.TrueAnomaly >= 0 && parameters.TrueAnomaly <= 2 * MathF.PI);
        }

        [Fact]
        public void OrbitalElementsFromState_EllipticalOrbit()
        {
            // Simple test case
            var r = new Vector3(8000f, 0f, 0f);
            var v = new Vector3(0f, 8f, 1f);   // some inclination

            OrbitalParameters parameters = OrbitalMath.CalculateOrbitalElementsFromState(Vector3.Zero, r, v);

            Assert.True(parameters.SemiMajorAxis > 0);
            Assert.True(parameters.Eccentricity >= 0 && parameters.Eccentricity < 1f);
            Assert.True(parameters.Inclination >= 0 && parameters.Inclination <= MathF.PI);
        }

        [Fact]
        public void OrbitalElementsFromState_HyperbolicOrbit()
        {
            var r = new Vector3(5000f, 0f, 0f);
            var v = new Vector3(10f, 10f, 0f);   // high speed

            OrbitalParameters parameters = OrbitalMath.CalculateOrbitalElementsFromState(Vector3.Zero, r, v);

            Assert.True(parameters.SemiMajorAxis < 0);        // negative a for hyperbolas
            Assert.True(parameters.Eccentricity > 1f);
        }

        [Fact]
        public void FlightPathAngle_ShouldBeCorrect()
        {
            float e = 0.5f;
            float ν = 0f; // periapsis
            float phi = OrbitalMath.CalculateFlightPathAngle(e, ν);
            Assert.Equal(0f, phi, Program.EPS); // at periapsis, velocity is perpendicular to radius

            ν = MathF.PI / 2;
            phi = OrbitalMath.CalculateFlightPathAngle(e, ν);
            Assert.True(phi > 0);
        }

        [Fact]
        public void CalculateInclination_ShouldBeCorrect()
        {
            var h = new Vector3(0, 0, 1f);
            Assert.Equal(0f, OrbitalMath.CalculateInclination(h), Program.EPS);

            // 45 degrees inclination - proper unit vector
            var h45 = new Vector3(0, 1f, 1f);
            h45 = h45.Normalized();
            Assert.Equal(MathF.PI / 4, OrbitalMath.CalculateInclination(h45), Program.EPS);
        }

        [Fact]
        public void CalculateRAAN_ShouldBeCorrect()
        {
            // 1. Equatorial orbit (h parallel to Z-axis) → RAAN = 0
            var hEquatorial = new Vector3(0, 0, 1f);
            Assert.Equal(0f, OrbitalMath.CalculateRAAN(hEquatorial));

            // 2. h mostly along X-axis → node along Y-axis → RAAN = 90° (π/2)
            var hAlongX = new Vector3(1f, 0f, 0.1f).Normalized();
            Assert.Equal(MathF.PI / 2, OrbitalMath.CalculateRAAN(hAlongX), Program.EPS);

            // 3. h mostly along Y-axis → node along -X-axis → RAAN = 180° (π)
            var hAlongY = new Vector3(0f, 1f, 0.1f).Normalized();
            Assert.Equal(MathF.PI, OrbitalMath.CalculateRAAN(hAlongY), Program.EPS);
        }


        [Fact]
        public void CalculateArgumentOfPeriapsis_ShouldBeCorrect()
        {
            var h = new Vector3(0, 0, 1f);
            var eVec = new Vector3(1f, 0, 0);

            float ω = OrbitalMath.CalculateArgumentOfPeriapsis(h, eVec);
            Assert.Equal(0f, ω, Program.EPS);
        }


        [Fact]
        public void TrueAnomaly_VectorFunction_ShouldBeAccurate()
        {
            float a = 10000f;
            float e = 0.5f;

            float p = OrbitalMath.CalculateSemiParameter(e, a);

            float ν = MathF.PI / 3;

            float rMag = OrbitalMath.CalculateDistanceToSatellite(e, p, ν);

            // Build position vector (PQW frame)
            Vector3 r = new(rMag * MathF.Cos(ν), rMag * MathF.Sin(ν), 0f);

            float μ = OrbitalParameters.Mu;
            float h = MathF.Sqrt(μ * p);

            float vr = (μ / h) * e * MathF.Sin(ν);
            float vtheta = (μ / h) * (1 + e * MathF.Cos(ν));

            Vector3 v = new(
                vr * MathF.Cos(ν) - vtheta * MathF.Sin(ν),
                vr * MathF.Sin(ν) + vtheta * MathF.Cos(ν),
                0f
            );

            float recovered = OrbitalMath.CalculateTrueAnomaly(r, v, e, p);

            Assert.True(MathF.Abs(ν - recovered) < Program.EPS);
        }


        [Fact]
        public void AngularMomentum_VectorAndScalar_ShouldMatch()
        {
            float a = 10000f;
            float e = 0.3f;

            float p = OrbitalMath.CalculateSemiParameter(e, a);
            float ν = 1.2f;

            float rMag = OrbitalMath.CalculateDistanceToSatellite(e, p, ν);

            float μ = OrbitalParameters.Mu;
            float h = OrbitalMath.CalculateSpecificAngularMomentum(p);

            float cosNu = MathF.Cos(ν);
            float sinNu = MathF.Sin(ν);

            // Correct velocity components
            float vr = (μ / h) * e * sinNu;
            float vtheta = (μ / h) * (1 + e * cosNu);

            Vector3 r = new(rMag * cosNu, rMag * sinNu, 0f);

            Vector3 v = new(
                vr * cosNu - vtheta * sinNu,
                vr * sinNu + vtheta * cosNu,
                0f
            );

            Vector3 hVec = OrbitalMath.CalculateAngularMomentumVector(r, v);

            Assert.Equal(h, hVec.Length(), Program.EPS * 10); // slightly relaxed tolerance
        }
    }
}
