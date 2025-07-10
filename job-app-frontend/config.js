// AutoJob Configuration
// Manages environment-specific settings for frontend

class AutoJobConfig {
    constructor() {
        this.environment = this.detectEnvironment();
        this.config = this.getConfig();
    }

    detectEnvironment() {
        const hostname = window.location.hostname;
        
        if (hostname === 'localhost' || hostname === '127.0.0.1') {
            return 'development';
        } else if (hostname.includes('azurewebsites.net')) {
            return 'production';
        } else {
            return 'staging';
        }
    }

    getConfig() {
        const configs = {
            development: {
                apiBaseUrl: 'http://localhost:7071/api',
                environment: 'development',
                debug: true,
                features: {
                    useFallbackData: true,
                    showDebugInfo: true
                }
            },
            staging: {
                apiBaseUrl: 'https://testhtw.azurewebsites.net/api',
                environment: 'staging', 
                debug: true,
                features: {
                    useFallbackData: false,
                    showDebugInfo: true
                }
            },
            production: {
                // Updated with your actual Azure Functions URL
                apiBaseUrl: 'https://testhtw.azurewebsites.net/api',
                functionKey: '', // No key needed - using Anonymous auth
                requiresFunctionKey: false, // NEW: Flag to indicate no auth needed
                environment: 'production',
                debug: false,
                features: {
                    useFallbackData: true, // Enable fallback for presentation
                    showDebugInfo: false
                }
            }
        };

        return configs[this.environment];
    }

    get apiBaseUrl() {
        return this.config.apiBaseUrl;
    }

    get debug() {
        return this.config.debug;
    }

    get features() {
        return this.config.features;
    }

    log(message, ...args) {
        if (this.debug) {
            console.log(`[AutoJob ${this.environment}]`, message, ...args);
        }
    }

    error(message, ...args) {
        console.error(`[AutoJob ${this.environment}]`, message, ...args);
    }
}

// Export for use in app.js
window.AutoJobConfig = AutoJobConfig;
