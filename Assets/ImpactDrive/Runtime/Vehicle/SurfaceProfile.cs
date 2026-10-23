using ImpactDrive.Physics;
using UnityEngine;

namespace ImpactDrive.Runtime
{
    public enum DriveSurface { DryAsphalt, WetAsphalt, Gravel, Ice, Concrete, Grass }

    /// <summary>Attach to a surface collider or its parent. Values come from the pure physics layer.</summary>
    [DisallowMultipleComponent]
    public sealed class SurfaceProfile : MonoBehaviour
    {
        public DriveSurface Surface = DriveSurface.DryAsphalt;

        private SurfaceParameters cachedParameters;
        private DriveSurface cachedSurface;

        public SurfaceParameters Parameters
        {
            get
            {
                if (cachedParameters != null && cachedSurface == Surface) return cachedParameters;
                cachedSurface = Surface;
                switch (Surface)
                {
                    case DriveSurface.WetAsphalt: return cachedParameters = SurfaceParameters.WetAsphalt;
                    case DriveSurface.Gravel: return cachedParameters = SurfaceParameters.Gravel;
                    case DriveSurface.Ice: return cachedParameters = SurfaceParameters.Ice;
                    case DriveSurface.Concrete: return cachedParameters = SurfaceParameters.Concrete;
                    case DriveSurface.Grass: return cachedParameters = SurfaceParameters.Grass;
                    default: return cachedParameters = SurfaceParameters.DryAsphalt;
                }
            }
        }
    }
}
