# Practical Calibration Guide for Parametric Bootstrap

This guide provides recommended values for the `ParametricBootstrap` strategy in `NinthBall`. These values are designed to provide a "Prudent Stress" test, capturing market risks without assuming extreme "asteroid" events.

## Calibration Table

| Parameter | Historical (1928-2023) | **Prudent Stress** (Recommended) | **Extreme Stress** (Previous) |
| :--- | :--- | :--- | :--- |
| **Skewness** | ~0.0 to -0.2 | **-0.3** | **-0.5** |
| **Kurtosis** | ~3.2 to 3.5 | **4.0** | **5.0** |
| **AutoCorr** | ~0.02 | **0.05** | **0.10** |

## Parameter Definitions & Reasoning

### Skewness
*   **Definition**: Measures the asymmetry of the return distribution.
*   **Reasoning**: Annual stock returns are slightly negatively skewed (more frequent small gains, rare large losses). A value of **-0.3** captures this "downside bias" effectively. Values more negative than -0.5 can produce mathematically extreme results (like -120% returns) when combined with high kurtosis.

### Kurtosis
*   **Definition**: Measures the "fatness" of the tails (frequency of extreme events).
*   **Reasoning**: A normal distribution has a Kurtosis of 3.0. A value of **4.0** models a "Fat Tail" scenario where extreme market crashes (like 2008) happen roughly once every 25 years, rather than once a century. This is a prudent assumption for a 30-year retirement plan.

### AutoCorrelation
*   **Definition**: Measures the "momentum" or "regime" of the market (how much this year's return depends on last year's).
*   **Reasoning**: While annual returns are mostly independent, a small positive value (**0.05**) models the tendency for market regimes (bull or bear) to persist slightly, adding another layer of realistic stress to the sequence of returns.
