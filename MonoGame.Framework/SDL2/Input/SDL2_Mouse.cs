#region License
/*
Microsoft Public License (Ms-PL)
MonoGame - Copyright © 2009 The MonoGame Team

All rights reserved.

This license governs use of the accompanying software. If you use the software, you accept this license. If you do not
accept the license, do not use the software.

1. Definitions
The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under 
U.S. copyright law.

A "contribution" is the original software, or any additions or changes to the software.
A "contributor" is any person that distributes its contribution under this license.
"Licensed patents" are a contributor's patent claims that read directly on its contribution.

2. Grant of Rights
(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.

3. Conditions and Limitations
(A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
(B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, 
your patent license from such contributor to the software ends automatically.
(C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution 
notices that are present in the software.
(D) If you distribute any portion of the software in source code form, you may do so only under this license by including 
a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object 
code form, you may only do so under a license that complies with this license.
(E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees
or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent
permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular
purpose and non-infringement.
*/
#endregion License

using System;
using SDL2;

namespace Microsoft.Xna.Framework.Input
{
    /// <summary>
    /// Allows reading position and button click information from mouse.
    /// </summary>
    public static class Mouse
    {
        #region Internal Properties and Variables
        
		internal static MouseState State;
        
        internal static IntPtr INTERNAL_sdlWindowHandle;

        public static IntPtr WindowHandle
        {
            get
            {
                return INTERNAL_sdlWindowHandle;
            }
        }
        
        // FIXME: SDL2_GameWindow needs to work with this!
        internal static int INTERNAL_MouseWheeel
        {
            get;
            set;
        }
        
        #endregion

        #region Public interface

        /// <summary>
        /// Gets mouse state information that includes position and button presses.
        /// </summary>
        /// <returns>Current state of the mouse.</returns>
        public static MouseState GetState()
        {
            int x, y;
			uint flags = SDL.SDL_GetMouseState(out x, out y);
   
            State.X = x;
            State.Y = y;
            
            State.LeftButton = (ButtonState) (flags & SDL.SDL_BUTTON_LMASK);
            State.RightButton = (ButtonState) ((flags & SDL.SDL_BUTTON_RMASK) >> 2);
            State.MiddleButton = (ButtonState) ((flags & SDL.SDL_BUTTON_MMASK) >> 1);
            
            State.ScrollWheelValue = INTERNAL_MouseWheeel;

            return State;
        }

        /// <summary>
        /// Sets mouse cursor's relative position to game-window.
        /// </summary>
        /// <param name="x">Relative horizontal position of the cursor.</param>
        /// <param name="y">Relative vertical position of the cursor.</param>
        public static void SetPosition(int x, int y)
        {
            State.X = x;
            State.Y = y;
            
            SDL.SDL_WarpMouseInWindow(INTERNAL_sdlWindowHandle, x, y);
        }

        #endregion // Public interface
    }
}

