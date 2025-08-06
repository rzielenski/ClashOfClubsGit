mergeInto(LibraryManager.library, {
    Authenticate: function(callback) {
        var domain = window.location.hostname;
        var username = Math.random().toString(36).substring(2);
        var challenge = new TextEncoder().encode(domain);
        navigator.credentials.get({
            publicKey: {
                challenge: challenge,
                rp: {
                    name: domain,
                },
                user: {
                    id: new Uint8Array(16),
                    name: username,
                    displayName: domain,
                },
                pubKeyCredParams: [
                    { type: 'public-key', alg: -7 },
                    { type: 'public-key', alg: -257 }
                ],
            }
        }).then(function(credential) {
            {{{ makeDynCall('vi', 'callback') }}} (true);
        }).catch(function(err) {
            {{{ makeDynCall('vi', 'callback') }}} (false);
        });
    }
});
