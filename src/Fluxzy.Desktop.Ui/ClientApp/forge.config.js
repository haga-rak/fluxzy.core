const util = require('util');
const exec = util.promisify(require('child_process').exec);
const fs = require('fs');
const rcedit = require('rcedit');


module.exports = {
    packagerConfig: {
        ignore: [
            "node_modules",
            "\\.angular",
            "\\.idea",
            "\\.run",
            "\\.vscode",
            "\\.npmrc",
            "^run$",
            "^/e2e",
            "^/src",
            "\\.gitignore",
            "\\.editorconfig",
            "forge.config.js",
            "[.](cmd|user|DotSettings|csproj|sln)$",
            "^/[^/]+[.](ts|js)$",
            "^/angular.json",
            "^/electron-builder.json",
            "^/tsconfig.json",
            "^/tsconfig.serve.json",
            "^/.eslintrc.json",
            "^/CHANGELOG.md",
            "[.]ts$",
            "[.]map$",
        ],
        win32metadata: {
            CompanyName: "Smartizy",
            ProductName: "Fluxzy",
        },
        icon: '.assets/icon'
    },
    hooks : {
        prePackage : async (forgeConfig, platform, arch) => {
        },
        postPackage: async (forgeConfig, options) => {
            if (process.platform !== 'win32')
                return;

            const fullName = options.outputPaths[0] + '\\fluxzy.exe';
            await rcedit(fullName, {
                'version-string': {
                    LegalCopyright: `Copyright © ${new Date().getFullYear()} Haga Rakotoharivelo`,
                    CompanyName : 'smartizy.com'
                },
            }) ;
        }
    },
    rebuildConfig: {},
    makers: [
        {
            name: '@electron-forge/maker-squirrel',
            config: {
                iconUrl: 'https://www.fluxzy.io/resources/icons/icon.ico',
                // The ICO file to use as the icon for the generated Setup.exe
                setupIcon: '.assets/icon.ico',
                certificateFile: '../../../build/certificates/staging.iampfx',
                certificatePassword: process.env.SIGNING_PFX_SERVER_PASSWORD,
            },
        },
        {
            name: '@electron-forge/maker-dmg',
            platforms: ['darwin'],
        },
        {
            name: '@electron-forge/maker-deb',
            config: {
                options: {
                    icon: '.assets/icon.png'
                }
            },
        },
        {
            // sudo dnf install rpm-build  #on fedora
            name: '@electron-forge/maker-rpm',
            config: {
                options: {
                    icon: '.assets/icon.png'
                }
            },
        }
    ],
    publishers: [
        {
            name: '@electron-forge/publisher-electron-release-server',
            config: {
                baseUrl: 'https://releases.fluxzy.io:4433',
                username: 'lilou',
                password: process.env.RELEASE_SERVER_PASSWORD,
                flavor: "default"
            }
        }
    ]
};
