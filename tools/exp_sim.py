"""Temporary XP simulation — delete or keep as design tool."""
import math

A, k = 0.5, 25


def L(d: float) -> float:
    return 1 + A * math.tanh(d / k)


def mult(pl: int, sl: int, sk: float, eq: float) -> float:
    d = sl - pl
    return L(d) * (1 + sk) * (1 + eq)


def base_linear(sl: int) -> float:
    return 10 * sl


def base_soft_early(sl: int) -> float:
    return 10 * (sl ** 0.92)


def base_hybrid(sl: int) -> float:
    # Lower linear coeff + quadratic tail: gentler at sl~5-15, ramps at 90+
    return 6 * sl + 0.08 * sl * sl


ROWS = [
    (10, 5, 0.01, 0.01),
    (10, 10, 0.01, 0.01),
    (15, 20, 0.02, 0.01),
    (15, 30, 0.02, 0.01),
    (15, 10, 0.02, 0.01),
    (20, 10, 0.03, 0.01),
    (20, 20, 0.03, 0.01),
    (20, 30, 0.03, 0.01),
    (30, 30, 0.03, 0.02),
    (30, 35, 0.03, 0.02),
    (30, 10, 0.03, 0.02),
    (35, 40, 0.03, 0.02),
]

HIGH = [
    (90, 95, 0.10, 0.04),
    (95, 100, 0.10, 0.04),
    (99, 100, 0.10, 0.04),
]

BASES = [
    ("linear 10*sl", base_linear),
    ("soft 10*sl^0.92", base_soft_early),
    ("hybrid 6*sl + 0.08*sl^2", base_hybrid),
]


def main() -> None:
    print("L(d) = 1 + 0.5*tanh(d/25), d = subLevel - playerLevel")
    print("final = base(subLevel) * L * (1+skill) * (1+equip)")
    print()

    for name, bf in BASES:
        print("===", name, "===")
        hdr = f"{'P':>4} {'S':>4} {'d':>5} {'L':>7} {'m':>8} {'base':>9} {'final':>10}"
        print(hdr)
        for pl, sl, sk, eq in ROWS:
            d = sl - pl
            m = mult(pl, sl, sk, eq)
            b = bf(sl)
            print(f"{pl:4d} {sl:4d} {d:5d} {L(d):7.3f} {m:8.4f} {b:9.1f} {m * b:10.1f}")
        print()

    print("--- High level (90+) ---")
    for name, bf in BASES:
        print(name)
        for pl, sl, sk, eq in HIGH:
            d = sl - pl
            m = mult(pl, sl, sk, eq)
            b = bf(sl)
            print(f"  P{pl} S{sl} d={d:+4d} L={L(d):.3f} base={b:8.0f} final={m * b:10.0f}")
        print()


if __name__ == "__main__":
    main()
