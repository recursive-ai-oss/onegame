
// Every test makes its own instance and device. The NVIDIA driver with the validation layer crashes now and then when several are created and
// destroyed at the same moment in one process, so the tests in this assembly run one after another (about seven seconds in all).
[assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.None)]
