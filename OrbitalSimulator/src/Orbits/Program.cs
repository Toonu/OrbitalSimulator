using System;
using Godot;

namespace OrbitalSimulator.src.Orbits {
    public class Program {
        public const float EPS = 1e-3f;

        /// <summary>
        /// Cannot be main as it would throw tantrum that there are multiple entry points in the project. So this is a workaround to test the orbital math via console app if needed.
        /// </summary>
        /// <param name="args">Arguments string[]</param>
        public static void Start(string[] args) {
            if (args.Length > 0) {
                GD.Print(args.Join());
            }

            float ftTokmConversion = 0.0003048f; // ft → km

            Vector3 r = new(4.1852f, 6.2778f, 10.463f);
            r *= 1e7f;
            r *= ftTokmConversion;

            Vector3 v = new(2.5936f, 5.1872f, 0);
            v *= 1e4f;
            v *= ftTokmConversion;

            OrbitalParameters op = OrbitalMath.CalculateOrbitalElementsFromState(Vector3.Zero, r, v);
            GD.Print(op.ToString());
        }
    }
}
