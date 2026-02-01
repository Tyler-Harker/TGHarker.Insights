(function(window) {
    'use strict';

    const Insights = {
        _propertyId: null,
        _apiEndpoint: null,
        _visitorId: null,
        _sessionId: null,
        _queue: [],
        _initialized: false,
        _debug: false,

        /**
         * Initialize the Insights SDK
         * @param {Object} config - Configuration object
         * @param {string} config.propertyId - Your property ID
         * @param {string} [config.endpoint] - API endpoint URL
         * @param {boolean} [config.autoPageView=true] - Auto-track page views
         * @param {boolean} [config.debug=false] - Enable debug logging
         */
        init: function(config) {
            if (!config.propertyId) {
                console.error('[Insights] propertyId is required');
                return;
            }

            this._propertyId = config.propertyId;
            this._apiEndpoint = config.endpoint || '/api/collect';
            this._debug = config.debug || false;
            this._visitorId = this._getOrCreateVisitorId();
            this._sessionId = this._getOrCreateSessionId();
            this._initialized = true;

            this._log('Initialized with propertyId:', this._propertyId);

            // Start session
            this._startSession();

            // Auto page view tracking
            if (config.autoPageView !== false) {
                this._trackPageView();
                this._setupHistoryTracking();
            }

            // Setup session end tracking
            this._setupSessionEnd();

            // Flush queued events
            this._flushQueue();
        },

        /**
         * Track a page view
         * @param {Object} [options] - Page view options
         * @param {string} [options.path] - Page path (defaults to current path)
         * @param {string} [options.title] - Page title (defaults to document.title)
         */
        pageview: function(options) {
            options = options || {};
            this._send('pageview', {
                path: options.path || window.location.pathname,
                title: options.title || document.title,
                referrer: document.referrer
            });
        },

        /**
         * Track a custom event
         * @param {string} category - Event category
         * @param {string} action - Event action
         * @param {string} [label] - Event label
         * @param {number} [value] - Event value
         */
        event: function(category, action, label, value) {
            this._send('event', {
                category: category,
                action: action,
                label: label,
                value: value
            });
        },

        /**
         * Identify the current visitor with a user ID
         * @param {string} userId - The user ID
         * @param {Object} [traits] - User traits
         */
        identify: function(userId, traits) {
            this._send('identify', {
                userId: userId,
                traits: traits || {}
            });
        },

        /**
         * Set custom attributes for the current visitor
         * @param {Object} attributes - Key-value pairs of attributes
         */
        setUserAttributes: function(attributes) {
            if (!attributes || typeof attributes !== 'object') {
                console.error('[Insights] setUserAttributes requires an object');
                return;
            }
            this._send('set_attributes', {
                attributes: attributes
            });
        },

        // ---- Internal Methods ----

        _getOrCreateVisitorId: function() {
            var storageKey = '_insights_vid';
            var visitorId = null;

            try {
                visitorId = localStorage.getItem(storageKey);
            } catch (e) {
                this._log('localStorage not available');
            }

            if (!visitorId) {
                visitorId = this._generateId();
                try {
                    localStorage.setItem(storageKey, visitorId);
                } catch (e) {}
            }

            return visitorId;
        },

        _getOrCreateSessionId: function() {
            var storageKey = '_insights_sid';
            var lastActivityKey = '_insights_last';
            var sessionTimeout = 30 * 60 * 1000; // 30 minutes
            var sessionId = null;
            var lastActivity = null;
            var now = Date.now();

            try {
                sessionId = sessionStorage.getItem(storageKey);
                lastActivity = sessionStorage.getItem(lastActivityKey);
            } catch (e) {
                this._log('sessionStorage not available');
            }

            // Check if session is still valid
            if (sessionId && lastActivity) {
                var timeSinceLastActivity = now - parseInt(lastActivity, 10);
                if (timeSinceLastActivity < sessionTimeout) {
                    this._updateLastActivity();
                    return sessionId;
                }
            }

            // Create new session
            sessionId = this._generateId();
            try {
                sessionStorage.setItem(storageKey, sessionId);
                this._updateLastActivity();
            } catch (e) {}

            return sessionId;
        },

        _updateLastActivity: function() {
            try {
                sessionStorage.setItem('_insights_last', Date.now().toString());
            } catch (e) {}
        },

        _generateId: function() {
            return 'xxxxxxxxxxxx4xxxyxxxxxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
                var r = Math.random() * 16 | 0;
                var v = c === 'x' ? r : (r & 0x3 | 0x8);
                return v.toString(16);
            });
        },

        _send: function(type, data) {
            if (!this._initialized) {
                this._queue.push({ type: type, data: data });
                return;
            }

            this._updateLastActivity();

            var payload = {
                type: type,
                propertyId: this._propertyId,
                visitorId: this._visitorId,
                sessionId: this._sessionId,
                timestamp: new Date().toISOString(),
                data: data,
                context: {
                    url: window.location.href,
                    userAgent: navigator.userAgent,
                    screenWidth: window.screen.width,
                    screenHeight: window.screen.height,
                    language: navigator.language,
                    timezone: Intl.DateTimeFormat().resolvedOptions().timeZone
                }
            };

            this._log('Sending:', type, data);

            // Use sendBeacon for reliability
            if (navigator.sendBeacon) {
                var blob = new Blob([JSON.stringify(payload)], { type: 'application/json' });
                navigator.sendBeacon(this._apiEndpoint, blob);
            } else {
                // Fallback to fetch
                fetch(this._apiEndpoint, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload),
                    keepalive: true
                }).catch(function(err) {
                    console.error('[Insights] Failed to send:', err);
                });
            }
        },

        _startSession: function() {
            var urlParams = new URLSearchParams(window.location.search);

            this._send('session_start', {
                referrer: document.referrer,
                landingPage: window.location.pathname,
                utmSource: urlParams.get('utm_source'),
                utmMedium: urlParams.get('utm_medium'),
                utmCampaign: urlParams.get('utm_campaign'),
                utmTerm: urlParams.get('utm_term'),
                utmContent: urlParams.get('utm_content')
            });
        },

        _setupSessionEnd: function() {
            var self = this;

            window.addEventListener('beforeunload', function() {
                self._send('session_end', {
                    exitPage: window.location.pathname
                });
            });

            // Also track visibility change (mobile browsers)
            document.addEventListener('visibilitychange', function() {
                if (document.visibilityState === 'hidden') {
                    self._send('session_end', {
                        exitPage: window.location.pathname
                    });
                }
            });
        },

        _setupHistoryTracking: function() {
            var self = this;

            // Track SPA navigation via History API
            var originalPushState = history.pushState;
            var originalReplaceState = history.replaceState;

            history.pushState = function() {
                originalPushState.apply(this, arguments);
                self._trackPageView();
            };

            history.replaceState = function() {
                originalReplaceState.apply(this, arguments);
                self._trackPageView();
            };

            window.addEventListener('popstate', function() {
                self._trackPageView();
            });
        },

        _trackPageView: function() {
            this.pageview();
        },

        _flushQueue: function() {
            while (this._queue.length > 0) {
                var item = this._queue.shift();
                this._send(item.type, item.data);
            }
        },

        _log: function() {
            if (this._debug) {
                console.log.apply(console, ['[Insights]'].concat(Array.prototype.slice.call(arguments)));
            }
        }
    };

    // Expose globally
    window.Insights = Insights;

    // Auto-init if config is present
    if (window.InsightsConfig) {
        Insights.init(window.InsightsConfig);
    }

})(window);
