namespace System.Numerics {
	/**
	 * Patch for lack of System.Numerics
	 */
	public struct Complex {

		public static Complex Conjugate(Complex c) {
			return new Complex(c.Real, -c.Imaginary);
		}

		public static Complex operator +(Complex a, Complex b) {
			return new Complex(a.re + b.re, a.im + b.im);
		}

		public static Complex operator *(Complex a, Complex b) {
			return new Complex(a.re * b.re - a.im * b.im, a.im * b.re + a.re * b.im);
		}
		public static Complex operator /(Complex a, double b) {
			return new Complex(a.re / b, a.re / b);
		}

		public static Complex Zero = new Complex(0, 0);
		public static Complex One = new Complex(1, 0);
		public static Complex ImaginaryOne = new Complex(0, 1);

		readonly double re;
		readonly double im;

		public Complex(double re, double im) {
			this.re = re;
			this.im = im;
		}

		public double Real {
			get {
				return re;
			}
		}

		public double Imaginary {
			get {
				return im;
			}
		}

		public double Magnitude {
			get {
				return Math.Sqrt(re * re + im * im);
			}
		}

		public double Phase {
			get {
				return Math.Atan2(im, re);
			}
		}
	}
}
