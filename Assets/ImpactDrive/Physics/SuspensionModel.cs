using System;

namespace ImpactDrive.Physics
{
    public static class SuspensionModel
    {
        /// <summary>
        /// Compression is rest length minus current length. Positive velocity compresses.
        /// This reduced model has a unilateral road reaction; it does not pull on the road.
        /// Bump stiffness begins in the last 15% of configured compression travel.
        /// </summary>
        public static float Evaluate(float compressionM, float compressionVelocityMps, VehicleSpec spec)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            Scalar.RequireFinite(compressionM, nameof(compressionM));
            Scalar.RequireFinite(compressionVelocityMps, nameof(compressionVelocityMps));
            if (compressionM <= 0f) return 0f;
            float damping = compressionVelocityMps >= 0f ? spec.CompressionDampingNsPerM : spec.ReboundDampingNsPerM;
            float bumpCompression = Math.Max(0f, compressionM - 0.85f * spec.SuspensionTravelM);
            return Math.Max(0f, spec.SpringRateNPerM * compressionM + damping * compressionVelocityMps + spec.BumpSpringRateNPerM * bumpCompression);
        }

        /// <summary>
        /// Add this force to left support and subtract it from right support. The pair has
        /// zero net force. The caller must handle lost wheel contact without road suction.
        /// </summary>
        public static float AntiRollForce(float leftCompressionM, float rightCompressionM, float rateNPerM)
        {
            Scalar.RequireFinite(leftCompressionM, nameof(leftCompressionM));
            Scalar.RequireFinite(rightCompressionM, nameof(rightCompressionM));
            Scalar.Range(rateNPerM, 0f, 500000f, nameof(rateNPerM));
            return (leftCompressionM - rightCompressionM) * rateNPerM;
        }
    }
}
