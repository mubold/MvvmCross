// MvxBindingFragmentAdapter.cs
// (c) Copyright Cirrious Ltd. http://www.cirrious.com
// MvvmCross is licensed using Microsoft Public License (Ms-PL)
// Contributions and inspirations noted in readme.md and license.txt
// 
// Project Lead - Stuart Lodge, @slodge, me@slodge.com

using System;
using Android.OS;
using Android.Support.V4.App;
using Cirrious.CrossCore;
using Cirrious.CrossCore.Core;
using Cirrious.CrossCore.Platform;
using Cirrious.MvvmCross.Droid.Support.Fragging.Fragments.EventSource;
using Cirrious.MvvmCross.Droid.Platform;
using Cirrious.MvvmCross.Droid.Views;
using Cirrious.MvvmCross.ViewModels;
using Cirrious.MvvmCross.Views;

namespace Cirrious.MvvmCross.Droid.Support.Fragging.Fragments
{
    public class MvxBindingFragmentAdapter
        : MvxBaseFragmentAdapter
    {
        public IMvxFragmentView FragmentView
        {
            get { return Fragment as IMvxFragmentView; }
        }

        public MvxBindingFragmentAdapter(IMvxEventSourceFragment eventSource)
            : base(eventSource)
        {
            if (!(eventSource is IMvxFragmentView))
                throw new ArgumentException("eventSource must be an IMvxFragmentView");
        }

        protected override void HandleCreateCalled(object sender, MvxValueEventArgs<Bundle> bundleArgs)
        {
            FragmentView.EnsureSetupInitialized();

            if (!FragmentView.GetType().IsCacheableFragmentAttribute())
                return;

            Bundle bundle = null;
            MvxViewModelRequest request = null;
            if (bundleArgs != null && bundleArgs.Value != null)
            {
                // saved state
                bundle = bundleArgs.Value;
            }
            else
            {
                var fragment = FragmentView as Fragment;
                if (fragment != null && fragment.Arguments != null)
                {
                    bundle = fragment.Arguments;
                    var json = bundle.GetString("__mvxViewModelRequest");
                    if (!string.IsNullOrEmpty(json))
                    {
                        IMvxNavigationSerializer serializer;
                        if (!Mvx.TryResolve(out serializer))
                        {
                            MvxTrace.Warning(
                                "Navigation Serializer not available, deserializing ViewModel Request will be hard");
                        }
                        else
                        {
                            request = serializer.Serializer.DeserializeObject<MvxViewModelRequest>(json);
                        }
                    }
                }
            }

            IMvxSavedStateConverter converter;
            if (!Mvx.TryResolve(out converter))
            {
                MvxTrace.Warning("Saved state converter not available - saving state will be hard");
            }
            else
            {
                if (bundle != null)
                {
                    var mvxBundle = converter.Read(bundle);
                    FragmentView.OnCreate(mvxBundle, request);
                }
            }
        }

        protected override void HandleCreateViewCalled(object sender,
                                               MvxValueEventArgs<MvxCreateViewParameters> args)
        {
            FragmentView.EnsureBindingContextIsSet(args.Value.Inflater);
        }

        protected override void HandleSaveInstanceStateCalled(object sender, MvxValueEventArgs<Bundle> bundleArgs)
        {
            if (!FragmentView.GetType().IsCacheableFragmentAttribute())
                return;

            var mvxBundle = FragmentView.CreateSaveStateBundle();
            if (mvxBundle != null)
            {
                IMvxSavedStateConverter converter;
                if (!Mvx.TryResolve(out converter))
                {
                    MvxTrace.Warning("Saved state converter not available - saving state will be hard");
                }
                else
                {
                    converter.Write(bundleArgs.Value, mvxBundle);
                }
            }
            var cache = Mvx.Resolve<IMvxMultipleViewModelCache>();
            cache.Cache(FragmentView.ViewModel, FragmentView.UniqueImmutableCacheTag);
        }

        protected override void HandleDestroyViewCalled(object sender, EventArgs e)
        {
            if (FragmentView.BindingContext != null)
            {
                FragmentView.BindingContext.ClearAllBindings();
            }
            base.HandleDestroyViewCalled(sender, e);
        }

        protected override void HandleDisposeCalled(object sender, EventArgs e)
        {
            if (FragmentView.BindingContext != null)
            {
                FragmentView.BindingContext.ClearAllBindings();
            }
            base.HandleDisposeCalled(sender, e);
        }
    }
}