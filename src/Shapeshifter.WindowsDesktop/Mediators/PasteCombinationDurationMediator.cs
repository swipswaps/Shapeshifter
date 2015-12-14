﻿namespace Shapeshifter.WindowsDesktop.Mediators
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;

    using Controls.Window.Interfaces;

    using Infrastructure.Events;
    using Infrastructure.Logging.Interfaces;
    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    using Services.Messages.Interceptors.Hotkeys.Interfaces;

    class PasteCombinationDurationMediator: IPasteCombinationDurationMediator
    {
        readonly IPasteHotkeyInterceptor pasteHotkeyInterceptor;

        readonly IConsumerThreadLoop threadLoop;

        readonly IThreadDelay threadDelay;

        readonly ILogger logger;

        readonly IMainThreadInvoker mainThreadInvoker;

        readonly CancellationTokenSource threadCancellationTokenSource;

        bool isCombinationDown;

        public event EventHandler<PasteCombinationDurationPassedEventArgument>
            PasteCombinationDurationPassed;

        public event EventHandler<PasteCombinationReleasedEventArgument> PasteCombinationReleased;

        public PasteCombinationDurationMediator(
            IPasteHotkeyInterceptor pasteHotkeyInterceptor,
            IConsumerThreadLoop threadLoop,
            IThreadDelay threadDelay,
            IMainThreadInvoker mainThreadInvoker,
            ILogger logger)
        {
            this.pasteHotkeyInterceptor = pasteHotkeyInterceptor;
            this.threadLoop = threadLoop;
            this.threadDelay = threadDelay;
            this.mainThreadInvoker = mainThreadInvoker;
            this.logger = logger;

            threadCancellationTokenSource = new CancellationTokenSource();
        }

        public bool IsConnected
            => threadLoop.IsRunning;

        bool IsCancellationRequested
            => threadCancellationTokenSource.Token.IsCancellationRequested;

        public bool IsCombinationHeldDown
            => Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.V);

        public bool IsOneCombinationKeyDown
            => Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.V);

        CancellationToken Token
            => threadCancellationTokenSource.Token;

        public int DurationInDeciseconds
            => 5;

        public void Connect(IWindow targetWindow)
        {
            if (IsConnected)
            {
                throw new InvalidOperationException(
                    "The clipboard combination mediator is already connected.");
            }

            InstallPasteHotkeyInterceptor();
        }

        async Task MonitorClipboardCombinationStateAsync()
        {
            await WaitForCombinationRelease();
            if (IsCancellationRequested)
            {
                return;
            }

            RegisterCombinationReleased();
        }

        void RegisterCombinationReleased()
        {
            isCombinationDown = false;
            if (PasteCombinationReleased != null)
            {
                mainThreadInvoker.Invoke(
                    () =>
                    PasteCombinationReleased(
                        this,
                        new PasteCombinationReleasedEventArgument
                            ()));
            }
        }

        async Task WaitForCombinationRelease()
        {
            var decisecondsPassed = 0;
            while (!IsCancellationRequested && IsOneCombinationKeyDown)
            {
                await threadDelay.ExecuteAsync(100);
                decisecondsPassed++;

                logger.Information($"Paste combination held down for {decisecondsPassed}.");

                RaiseDurationPassedEventIfNeeded(decisecondsPassed);
            }
        }

        void RaiseDurationPassedEventIfNeeded(int decisecondsPassed)
        {
            if ((decisecondsPassed != DurationInDeciseconds) || (PasteCombinationDurationPassed == null))
            {
                return;
            }

            mainThreadInvoker.Invoke(
                () =>
                PasteCombinationDurationPassed(
                    this,
                    new PasteCombinationDurationPassedEventArgument
                        ()));
            logger.Information("Paste duration passed event raised.");
        }

        void InstallPasteHotkeyInterceptor()
        {
            pasteHotkeyInterceptor.HotkeyFired += PasteHotkeyInterceptor_PasteHotkeyFired;
        }

        void UninstallPasteHotkeyInterceptor()
        {
            pasteHotkeyInterceptor.HotkeyFired -= PasteHotkeyInterceptor_PasteHotkeyFired;
        }

        void PasteHotkeyInterceptor_PasteHotkeyFired(object sender, HotkeyFiredArgument e)
        {
            if (!isCombinationDown)
            {
                logger.Information(
                    "Paste combination duration mediator reacted to paste hotkey.",
                    1);

                isCombinationDown = true;
                threadLoop.Notify(MonitorClipboardCombinationStateAsync, Token);
            }
            else
            {
                logger.Information(
                    "Paste combination duration mediator ignored paste hotkey because the paste combination was already held down.",
                    1);
            }
        }

        public void Disconnect()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException(
                    "The clipboard combination mediator is already disconnected.");
            }

            threadCancellationTokenSource.Cancel();
            UninstallPasteHotkeyInterceptor();
        }
    }
}