using Godot;
using OrbitalSimulator.src.Orbits;

namespace OrbitalSimulator.Tests {
    public class OrbitalMathTests {
        [Fact]
        public void SemiParameter_ShouldBeCorrect() {
            float a = 10000;
            float e = 0.1f;

            var result = OrbitalMath.CalculateSemiParameter(e, a);

            Assert.Equal(9900f, result, Program.EPS);
        }

        [Fact]
        public void SemiMinorAxis_ShouldBeCorrect() {
            float a = 10000;
            float e = 0.5f;

            var result = OrbitalMath.CalculateSemiMinorAxis(e, a);

            Assert.Equal(a * MathF.Sqrt(1 - e * e), result, Program.EPS);
        }


        [Fact]
        public void Apoapsis_ShouldBeCorrect() {
            float a = 10000;
            float e = 0.2f;

            var result = OrbitalMath.CalculateApoapsis(e, a);

            Assert.Equal(12000f, result, Program.EPS);
        }

        [Fact]
        public void Periapsis_ShouldBeCorrect() {
            float a = 10000;
            float e = 0.2f;

            var result = OrbitalMath.CalculatePeriapsis(e, a);

            Assert.Equal(8000f, result, Program.EPS);
        }

        [Fact]
        public void SpecificEnergy_FromA_ShouldBeCorrect() {
            float a = 10000f;

            var result = OrbitalMath.CalculateSpecificOrbitalEnergy(a);

            Assert.Equal(-(OrbitalParameters.mu / (2 * a)), result, Program.EPS);
        }

        [Fact]
        public void OrbitPeriod_ShouldBeCorrect() {
            float a = 7000f;

            float expected = 2 * MathF.PI * MathF.Sqrt(MathF.Pow(a, 3) / OrbitalParameters.mu);

            var result = OrbitalMath.CalculateOrbitalPeriod(a);

            Assert.Equal(expected, result, Program.EPS);
        }

        [Fact]
        public void DistanceToSatellite_ShouldBeCorrect() {
            float e = 0.1f;
            float p = 10000;
            float v = 0;

            var result = OrbitalMath.CalculateDistanceToSatellite(e, p, v);

            Assert.Equal(p / (1 + e), result, Program.EPS);
        }

        [Fact]
        public void Radius_ShouldMatch_PeriodicPoints() {
            float a = 10000f;
            float e = 0.2f;

            float p = OrbitalMath.CalculateSemiParameter(e, a);

            float r_pe = OrbitalMath.CalculateDistanceToSatellite(e, p, 0);
            float r_ap = OrbitalMath.CalculateDistanceToSatellite(e, p, MathF.PI);

            Assert.Equal(a * (1 - e), r_pe, Program.EPS);
            Assert.Equal(a * (1 + e), r_ap, Program.EPS);
        }

        [Fact]
        public void Energy_Formulas_ShouldMatch() {
            float a = 10000f;

            float r = a; // circular-ish assumption
            float v = MathF.Sqrt(OrbitalParameters.mu / r);

            float e1 = OrbitalMath.CalculateSpecificOrbitalEnergy(r: r, v: v);
            float e2 = OrbitalMath.CalculateSpecificOrbitalEnergy(a);

            Assert.Equal(e1, e2, Program.EPS);
        }

        [Fact]
        public void CircularOrbit_ShouldBehaveCorrectly() {
            float a = 10000f;
            float e = 0f;

            float p = OrbitalMath.CalculateSemiParameter(e, a);
            float b = OrbitalMath.CalculateSemiMinorAxis(e, a);

            Assert.Equal(a, p, Program.EPS);
            Assert.Equal(a, b, Program.EPS);
        }

        [Fact]
        public void HighEccentricity_ShouldRemainStable() {
            float a = 10000f;
            float e = 0.99f;

            float p = OrbitalMath.CalculateSemiParameter(e, a);
            float pe = OrbitalMath.CalculatePeriapsis(e, a);

            Assert.True(p > 0);
            Assert.True(pe > 0);
        }

        [Fact]
        public void Ellipse_ShouldBeSymmetric() {
            float a = 10000f;
            float e = 0.3f;

            float p = OrbitalMath.CalculateSemiParameter(e, a);

            float v = 0.7f;

            float r1 = OrbitalMath.CalculateDistanceToSatellite(e, p, v);
            float r2 = OrbitalMath.CalculateDistanceToSatellite(e, p, -v);

            Assert.Equal(r1, r2, Program.EPS);
        }

        [Fact]
        public void AxesRelationship_ShouldHold() {
            float a = 10000f;
            float e = 0.6f;

            float b = OrbitalMath.CalculateSemiMinorAxis(e, a);
            float c = OrbitalMath.CalculateLinearEccentricity(a, e);

            Assert.Equal(a * a, b * b + c * c, Program.EPS);
        }

        [Fact]
        public void GetOrbitType_ShouldClassifyCorrectly() {
            Assert.Equal(OrbitType.Elliptical, OrbitalMath.GetOrbitType(0f));
            Assert.Equal(OrbitType.Elliptical, OrbitalMath.GetOrbitType(0.8f));
            Assert.Equal(OrbitType.Parabolic, OrbitalMath.GetOrbitType(1f));
            Assert.Equal(OrbitType.Hyperbolic, OrbitalMath.GetOrbitType(1.2f));
        }

        [Fact]
        public void SemiMinorAxis_ShouldSupportHyperbolic() {
            float a = 10000f;
            float e = 1.5f;

            var result = OrbitalMath.CalculateSemiMinorAxis(e, a);
            Assert.True(result > 0);
            // b = a * sqrt(e² - 1)
            Assert.Equal(a * MathF.Sqrt(e * e - 1), result, Program.EPS);
        }

        [Fact]
        public void Apoapsis_ShouldThrowForNonElliptical() {
            Assert.Throws<ArgumentException>(() => OrbitalMath.CalculateApoapsis(1.0f, 10000f));

            Assert.Throws<ArgumentException>(() => OrbitalMath.CalculateApoapsis(1.5f, 10000f));
        }

        [Fact]
        public void VisViva_Speed_ShouldBeCorrect() {
            float a = 10000f;
            float e = 0.3f;
            //float p = OrbitalMathF.CalculateSemiParameter(e, a);

            // At periapsis
            float r_pe = OrbitalMath.CalculatePeriapsis(e, a);
            float v_pe = OrbitalMath.CalculateOrbitalVelocity(a, r_pe);
            Assert.True(v_pe > 0);

            // At apoapsis
            float r_ap = OrbitalMath.CalculateApoapsis(e, a);
            float v_ap = OrbitalMath.CalculateOrbitalVelocity(a, r_ap);
            Assert.True(v_ap > 0);
            Assert.True(v_pe > v_ap); // faster at periapsis
        }


        [Fact]
        public void VisViva_Hyperbolic_ShouldWork() {
            float a = -8000f;        // negative a for hyperbolas
            float e = 1.2f;
            //float p = OrbitalMathF.CalculateSemiParameter(e, a);

            float r = OrbitalMath.CalculatePeriapsis(e, a);
            float speed = OrbitalMath.CalculateOrbitalVelocity(a, r);

            Assert.True(speed > 0);
        }


        [Fact]
        public void SpecificEnergy_From_r_v_ShouldMatch_From_a() {
            float a = 10000f;
            float r = 11000f;

            float speed = OrbitalMath.CalculateOrbitalVelocity(a, r);
            float energy1 = OrbitalMath.CalculateSpecificOrbitalEnergy(r: r, v: speed);
            float energy2 = OrbitalMath.CalculateSpecificOrbitalEnergy(a);

            Assert.Equal(energy1, energy2, Program.EPS);
        }


        [Fact]
        public void TrueAnomaly_FromRadiusAndVelocity_ShouldBeAccurate() {
            float a = 10000f;
            float e = 0.5f;

            float p = OrbitalMath.CalculateSemiParameter(e, a);

            float ν = MathF.PI / 3; // 60°

            float rMag = OrbitalMath.CalculateDistanceToSatellite(e, p, ν);

            // Construct consistent velocity using orbital mechanics
            float μ = OrbitalParameters.mu;
            float h = MathF.Sqrt(μ * p);

            float vr = (μ / h) * e * MathF.Sin(ν);

            float nuRecovered = OrbitalMath.CalculateTrueAnomalyFromRadius(e, p, rMag, vr);

            Assert.True(MathF.Abs(ν - nuRecovered) < Program.EPS);
        }


        [Fact]
        public void SemiParameter_FromAngularMomentum_ShouldMatch_FromAAndE() {
            float a = 12000f;
            float e = 0.3f;

            float p1 = OrbitalMath.CalculateSemiParameter(e, a);

            float h = MathF.Sqrt(OrbitalParameters.mu * p1);
            float p2 = OrbitalMath.CalculateSemiParameterFromAngularMomentum(h);

            Assert.Equal(p1, p2, Program.EPS);
        }


        [Fact]
        public void Distance_ShouldBeSymmetricAroundZero() {
            float a = 10000f;
            float e = 0.6f;

            float p = OrbitalMath.CalculateSemiParameter(e, a);

            for (float ν = 0.1f; ν < MathF.PI; ν += 0.2f) {
                float r1 = OrbitalMath.CalculateDistanceToSatellite(e, p, ν);
                float r2 = OrbitalMath.CalculateDistanceToSatellite(e, p, -ν);

                Assert.Equal(r1, r2, Program.EPS);
            }
        }


        [Fact]
        public void NearParabolicOrbit_ShouldNotCrash() {
            float a = 10000f;
            float e = 0.999f;

            float p = OrbitalMath.CalculateSemiParameter(e, a);

            float r = OrbitalMath.CalculateDistanceToSatellite(e, p, 0.5f);

            Assert.True(r > 0);
        }


        [Fact]
        public void NearCircularOrbit_ShouldRemainStable() {
            float a = 10000f;
            float e = 1e-6f;

            float p = OrbitalMath.CalculateSemiParameter(e, a);

            float r = OrbitalMath.CalculateDistanceToSatellite(e, p, 1.0f);

            Assert.True(r > 0);
        }


        [Fact]
        public void FlightPathAngle_ShouldBeZero_AtPeriapsisAndApoapsis() {
            float e = 0.5f;

            float phiPe = OrbitalMath.CalculateFlightPathAngle(e, 0f);
            float phiAp = OrbitalMath.CalculateFlightPathAngle(e, MathF.PI);

            Assert.Equal(0f, phiPe, Program.EPS);
            Assert.Equal(0f, phiAp, Program.EPS);
        }


        [Fact]
        public void AngularMomentum_ShouldMatch_FromP() {
            float a = 10000f;
            float e = 0.4f;

            float p = OrbitalMath.CalculateSemiParameter(e, a);

            float h = OrbitalMath.CalculateSpecificAngularMomentum(p);

            Assert.Equal(p, h * h / OrbitalParameters.mu, Program.EPS);
        }


        [Fact]
        public void OrbitalVelocity_CircularOrbit_ShouldEqualCircularVelocity() {
            float a = 8000f;
            float r = a;

            float v1 = OrbitalMath.CalculateOrbitalVelocity(a, r);
            float v2 = OrbitalMath.CalculateCircularVelocity(r);

            Assert.Equal(v2, v1, Program.EPS);
        }


        [Fact]
        public void HighEccentricity_Hyperbolic_ShouldRemainStable() {
            float a = -10000f; // Negative a for hyperbolic orbits!
            float e = 1.5f;

            float p = OrbitalMath.CalculateSemiParameter(e, a);
            float pe = OrbitalMath.CalculatePeriapsis(e, a);

            Assert.True(p > 0);
            Assert.True(pe > 0);
        }


        [Fact]
        public void CalculateSemiMajorAxis_FromApPe_ShouldBeCorrect() {
            float ap = 12000f;
            float pe = 8000f;

            float a = OrbitalMath.CalculateSemiMajorAxis(ap, pe);
            Assert.Equal(10000f, a, Program.EPS);
        }


        [Fact]
        public void CalculateSemiMajorAxis_FromPeriod_ShouldBeCorrect() {
            float a = 7000f;
            float expectedPeriod = OrbitalMath.CalculateOrbitalPeriod(a);

            float aFromPeriod = OrbitalMath.CalculateSemiMajorAxisFromPeriod(expectedPeriod);

            Assert.Equal(a, aFromPeriod, 0.01); //Looser tolerance as its more aproximation from time
        }


        [Fact]
        public void MeanOrbitalVelocity_Circular_ShouldBeCorrect() {
            float r = 7000f;

            float v = OrbitalMath.CalculateCircularVelocity(r);
            float expected = MathF.Sqrt(OrbitalParameters.mu / r);

            Assert.Equal(expected, v, Program.EPS);
        }


        [Fact]
        public void MeanOrbitalVelocity_FromPeriod_ShouldBeCorrect() {
            float a = 7000f;
            float period = OrbitalMath.CalculateOrbitalPeriod(a);

            float v = OrbitalMath.CalculateCircularVelocityFromPeriod(a, period);
            Assert.True(v > 0);
        }
    }
}