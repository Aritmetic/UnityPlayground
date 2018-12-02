using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VehiclePhysics
{
    public abstract class VehicleComponent
    {
        /// <summary>
        /// Update every frame.
        /// </summary>
        abstract public void Update();

        /// <summary>
        /// Update every physics Update.
        /// </summary>
        abstract public void FixedUpdate();
        
    }
}
