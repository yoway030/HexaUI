namespace HexaUI
{
    using HexaUI.Input;
    using Hexa.NET.ImGui.Backends.SDL2;
    using Silk.NET.SDL;
    using System;
    using System.Collections.Generic;

    public enum Backend
    {
        OpenGL,
        DirectX,
        Vulkan,
        Metal,
    }

    public unsafe class App
    {
        protected static App instance { get; set; } = null!;

        public static readonly Sdl sdl = Sdl.GetApi();

        protected static uint mainWindowId;

        protected static CoreWindow mainWindow = null!;
        public static CoreWindow MainWindow { get => mainWindow; protected set => mainWindow = value; }

        public static Backend Backend { get; protected set; }

        public static void RegisterHook(Func<Event, bool> hook)
        {
            if (instance == null)
            {
                throw new InvalidOperationException("instance is not initialized. Call Init() first.");
            }

            instance.RegisterHookImpl(hook);
        }

        virtual public void RegisterHookImpl(Func<Event, bool> hook)
        {
            throw new NotImplementedException("This method should be overridden in derived classes.");
        }

        public static void RemoveHook(Func<Event, bool> hook)
        {
            if (instance == null)
            {
                throw new InvalidOperationException("instance is not initialized. Call Init() first.");
            }

            instance.RemoveHookImpl(hook);
        }

        virtual public void RemoveHookImpl(Func<Event, bool> hook)
        {
            throw new NotImplementedException("This method should be overridden in derived classes.");
        }
    }
}