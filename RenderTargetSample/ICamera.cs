using Microsoft.Xna.Framework;

namespace RenderingTargetSample
{
    /// <summary>
    /// The interface for the camera, describing what portion of the level is being rendered.
    /// Responsible for transforming level to screen coordinates and vice-verca.
    /// </summary>
    public interface ICamera
    {
        /// <summary>
        /// The projection matrix that represents the current camera.
        /// </summary>
        Matrix Projection { get; }

        /// <summary>
        /// The view matrix that represents the current camera.
        /// </summary>
        Matrix View { get; }


        /// <summary>
        /// Updates the cameras internal values.
        /// </summary>
        /// <param name="dt"></param>
        void Update(GameTime dt);
    }
}