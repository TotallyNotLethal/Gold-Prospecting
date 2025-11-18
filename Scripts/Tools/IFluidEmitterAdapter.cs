/// <summary>
/// Small abstraction that lets the pan talk to either an Obi emitter, a VFX Graph
/// system, or any other custom particle system without taking a hard compile-time
/// dependency on those packages.
/// </summary>
public interface IFluidEmitterAdapter
{
    void SetBirthRate(float birthRatePerSecond);
}
