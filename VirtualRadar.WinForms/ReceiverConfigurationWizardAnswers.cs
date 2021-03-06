﻿// Copyright © 2014 onwards, Andrew Whewell
// All rights reserved.
//
// Redistribution and use of this software in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
//    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//    * Neither the name of the author nor the names of the program's contributors may be used to endorse or promote products derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE AUTHORS OF THE SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VirtualRadar.Interface.View;
using System.ComponentModel;
using VirtualRadar.Interface.Settings;
using System.Linq.Expressions;
using VirtualRadar.Interface;

namespace VirtualRadar.WinForms
{
    /// <summary>
    /// The WinForms implementation of <see cref="IReceiverConfigurationWizardAnswers"/>.
    /// </summary>
    class ReceiverConfigurationWizardAnswers : IReceiverConfigurationWizardAnswers, INotifyPropertyChanged
    {
        private ReceiverClass _ReceiverClass;
        /// <summary>
        /// See interface docs.
        /// </summary>
        public ReceiverClass ReceiverClass
        {
            get { return _ReceiverClass; }
            set { SetField(ref _ReceiverClass, value, () => ReceiverClass); }
        }

        private SdrDecoder _SdrDecoder;
        /// <summary>
        /// See interface docs.
        /// </summary>
        public SdrDecoder SdrDecoder
        {
            get { return _SdrDecoder; }
            set { SetField(ref _SdrDecoder, value, () => SdrDecoder); }
        }

        private DedicatedReceiver _DedicatedReceiver;
        /// <summary>
        /// See interface docs.
        /// </summary>
        public DedicatedReceiver DedicatedReceiver
        {
            get { return _DedicatedReceiver; }
            set { SetField(ref _DedicatedReceiver, value, () => DedicatedReceiver); }
        }

        private ConnectionType _ConnectionType;
        /// <summary>
        /// See interface docs.
        /// </summary>
        public ConnectionType ConnectionType
        {
            get { return _ConnectionType; }
            set { SetField(ref _ConnectionType, value, () => ConnectionType); }
        }

        private KineticConnection _KineticConnection;
        /// <summary>
        /// Gets or sets an enumeration version of <see cref="IsUsingBaseStation"/>, suitable
        /// for binding to a radio button.
        /// </summary>
        public KineticConnection KineticConnection
        {
            get { return _KineticConnection; }
            set { SetField(ref _KineticConnection, value, () => KineticConnection); }
        }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public bool IsUsingBaseStation
        {
            get { return KineticConnection == KineticConnection.BaseStation; }
            set { KineticConnection = value ? KineticConnection.BaseStation : KineticConnection.DirectToHardware; }
        }

        private YesNo _UseLoopbackAddress;
        /// <summary>
        /// Gets or sets an enumeration version of <see cref="IsLoopback"/>, suitable for
        /// binding to a radio button.
        /// </summary>
        public YesNo UseLoopbackAddress
        {
            get { return _UseLoopbackAddress; }
            set { SetField(ref _UseLoopbackAddress, value, () => UseLoopbackAddress); }
        }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public bool IsLoopback
        {
            get { return UseLoopbackAddress == YesNo.Yes; }
            set { UseLoopbackAddress = value ? YesNo.Yes : YesNo.No; }
        }

        private string _NetworkAddress;
        /// <summary>
        /// See interface docs.
        /// </summary>
        public string NetworkAddress
        {
            get { return _NetworkAddress; }
            set { SetField(ref _NetworkAddress, value, () => NetworkAddress); }
        }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            EventHelper.Raise(PropertyChanged, this, args);
        }

        protected bool SetField<T>(ref T field, T value, Expression<Func<T>> selectorExpression)
        {
            if(EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;

            if(selectorExpression == null) throw new ArgumentNullException("selectorExpression");
            MemberExpression body = selectorExpression.Body as MemberExpression;
            if(body == null) throw new ArgumentException("The body must be a member expression");
            OnPropertyChanged(new PropertyChangedEventArgs(body.Member.Name));

            return true;
        }
    }
}
