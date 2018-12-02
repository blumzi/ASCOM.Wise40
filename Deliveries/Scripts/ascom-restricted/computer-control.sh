
# vim:se ai terse nows ts=4 sts=4 sw=4 expandtab:

computercontrol=${ascom_server}/safetymonitor/0

function computercontrol_drivername() {
    GET ${computercontrol}/Name
}

function computercontrol_driverversion() {
    GET ${computercontrol}/DriverVersion
}

function computercontrol_driverinfo() {
    GET ${computercontrol}/DriverInfo
}

function computercontrol_issafe() {
    GET ${computercontrol}/issafe
}

function computercontrol_unsafereasons() {
    PUT ${computercontrol}/Action "Action=unsafereasons&Parameters="
}

function computercontrol_supportedactions() {
    GET ${computercontrol}/SupportedActions
}

