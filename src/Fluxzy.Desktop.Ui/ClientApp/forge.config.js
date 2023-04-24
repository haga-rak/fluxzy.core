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
            "^e2e$",
            "/src/",
            "\\.gitignore",
            "\\.editorconfig",
            "forge.config.js",
            "[.](cmd|user|DotSettings|csproj|sln)$"
        ],
        win32metadata: {
            CompanyName: "Smartizy",
            ProductName: "Fluxzy",
        },
        icon: '.assets/icon'
    },
    hooks : {
        prePackage : async (forgeConfig, platform, arch) => {
        //    await exec('npm run build:prod');
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
            },
        },
        {
            name: '@electron-forge/maker-zip',
            platforms: ['darwin'],
        },
        {
            name: '@electron-forge/maker-deb',
            config: {
                options: {
                    icon: '.assets/icon.png'
                }
            },
        }
    ],
};