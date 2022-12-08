const util = require('util');
const exec = util.promisify(require('child_process').exec);


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
        }
    },
    hooks : {
        prePackage : async (forgeConfig, platform, arch) => {
        //    await exec('npm run build:prod');
        }
    },
    rebuildConfig: {},
    makers: [
        {
            name: '@electron-forge/maker-squirrel',
            config: {},
        },
        {
            name: '@electron-forge/maker-zip',
            platforms: ['darwin'],
        },
        {
            name: '@electron-forge/maker-deb',
            config: {},
        },
        {
            name: '@electron-forge/maker-rpm',
            config: {},
        },
    ],
};
