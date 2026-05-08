using Godot;
using OrbitalSimulator.src.Orbits;

namespace OrbitalSimulator.Tests {
    public class OrbitalPropagatorTests {
        #region KEPLER SOLVER TESTS

        [Fact]
        public void SolveKepler_ShouldSatisfyEquation() {
            float e = 0.5f;
            float M = 1.2f;

            float E = OrbitalPropagator.SolveKepler(M, e);

            float reconstructedM = E - e * MathF.Sin(E);

            Assert.Equal(M, reconstructedM, Program.EPS);
        }

        [Fact]
        public void SolveKeplerHyperbolic_ShouldSatisfyEquation() {
            float e = 1.5f;
            float M = 1.0f;

            float H = OrbitalPropagator.SolveKeplerHyperbolic(M, e);

            float reconstructedM = e * MathF.Sinh(H) - H;

            Assert.Equal(M, reconstructedM, Program.EPS);
        }

        #endregion

        #region PROPAGATION TESTS

        [Fact]
        public void Propagate_ZeroTime_ShouldReturnSameState() {
            OrbitalParameters orbit = new(Vector3.Zero, 10000f, 0.3f, 1.0f, 0.3f, 1.0f, 0.5f);

            var (r1, v1) = OrbitalMath.CalculateOrbitalVectorsFromParameters(orbit);
            var (r2, v2) = OrbitalPropagator.Propagate(orbit, 0f);

            Assert.True((r1 - r2).Length() < Program.EPS);
            Assert.True((v1 - v2).Length() < Program.EPS);
        }

        [Fact]
        public void Propagate_Energy_ShouldBeConserved_Elliptical() {
            OrbitalParameters orbit = new(Vector3.Zero, 12000f, 0.4f, 0.5f, 0.2f, 0.7f, 1.1f);

            var (r1, v1) = OrbitalMath.CalculateOrbitalVectorsFromParameters(orbit);

            float E1 = OrbitalMath.CalculateSpecificOrbitalEnergy(r1.Length(), v1.Length());

            var (r2, v2) = OrbitalPropagator.Propagate(orbit, 500f);

            float E2 = OrbitalMath.CalculateSpecificOrbitalEnergy(r2.Length(), v2.Length());

            Assert.Equal(E1, E2, Program.EPS);
        }

        [Fact]
        public void Propagate_Energy_ShouldBeConserved_Hyperbolic() {
            OrbitalParameters orbit = new(Vector3.Zero, -8000f, 1.5f, 0.3f, 0.2f, 0.7f, 1.1f);

            var (r1, v1) = OrbitalMath.CalculateOrbitalVectorsFromParameters(orbit);

            float E1 = OrbitalMath.CalculateSpecificOrbitalEnergy(r1.Length(), v1.Length());

            var (r2, v2) = OrbitalPropagator.Propagate(orbit, 200f);

            float E2 = OrbitalMath.CalculateSpecificOrbitalEnergy(r2.Length(), v2.Length());

            Assert.Equal(E1, E2, Program.EPS);
        }

        #endregion

        #region VISUALIZATION TESTS

        [Fact]
        public void GenerateEllipsePoints_ShouldFormClosedOrbit() {
            OrbitalParameters orbit = new(Vector3.Zero, 10000f, 0.2f, 0f, 0.3f, 1.0f, 0.5f);

            var points = OrbitalPropagator.GenerateEllipsePoints(orbit, 200);

            Assert.Equal(201, points.Count);

            Vector3 first = points[0];
            Vector3 last = points[^1];

            // First and last should be close (loop)
            Assert.True((first - last).Length() < 1);
        }

        [Fact]
        public void GenerateEllipsePoints_RadiusMatchesOrbit() {
            float a = 10000f;
            float e = 0.3f;

            OrbitalParameters orbit = new(Vector3.Zero, a, e, 0f, 0f, 0f, 0f);

            var points = OrbitalPropagator.GenerateEllipsePoints(orbit, 50);

            foreach (var point in points) {
                float r = point.Length();

                // Check that r follows expected bounds
                Assert.True(r >= a * (1 - e) - 10f);
                Assert.True(r <= a * (1 + e) + 10f);
            }
        }

        [Fact]
        public void GenerateHyperbolicPoints_ShouldProduceValidPoints() {
            OrbitalParameters orbit = new(Vector3.Zero, -8000f, 1.2f, 0f, 0.3f, 0.5f, 0.6f);

            var points = OrbitalPropagator.GenerateHyperbolicPoints(orbit, 100);

            Assert.True(points.Count > 0);

            foreach (var p in points) {
                Assert.False(float.IsNaN(p.X));
                Assert.False(float.IsNaN(p.Y));
                Assert.False(float.IsNaN(p.Z));
            }
        }

        [Fact]
        public void HyperbolicCurve_ShouldNotClose() {
            OrbitalParameters orbit = new(Vector3.Zero, -10000f, 1.5f, 0f, 0.2f, 0.5f, 0.3f);

            var points = OrbitalPropagator.GenerateHyperbolicPoints(orbit, 100);

            Vector3 first = points[0];
            Vector3 last = points[^1];

            // Should NOT loop
            Assert.True((first - last).Length() > 1000f);
        }
        #endregion
    }
}