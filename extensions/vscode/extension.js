const vscode = require("vscode");
const childProcess = require("child_process");

function activate(context) {
  for (const command of ["init", "validate", "package", "sign", "publish"]) {
    context.subscriptions.push(vscode.commands.registerCommand(`theunlocker.${command}`, () => run(command)));
  }
}

function run(command) {
  const folder = vscode.workspace.workspaceFolders?.[0]?.uri.fsPath;
  if (!folder) {
    vscode.window.showWarningMessage("Open a workspace folder first.");
    return;
  }

  const terminal = vscode.window.createTerminal("TheUnlocker");
  terminal.show();
  if (command === "init") {
    terminal.sendText(`unlocker-mod init "${folder}" my-mod`);
    return;
  }

  if (command === "package") {
    terminal.sendText(`unlocker-mod package "${folder}" "${folder}/artifacts"`);
    return;
  }

  if (command === "publish") {
    terminal.sendText(`unlocker-mod publish "${folder}/artifacts/my-mod-1.0.0.zip" "${folder}/repository-index.json" https://example.com/my-mod.zip`);
    return;
  }

  if (command === "sign") {
    terminal.sendText(`unlocker-mod sign "${folder}/bin/Debug/net8.0" "${folder}/keys/private.pem"`);
    return;
  }

  terminal.sendText(`unlocker-mod ${command} "${folder}"`);
}

module.exports = { activate };
