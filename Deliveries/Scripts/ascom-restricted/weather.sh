
# vim:se ai terse nows ts=4 sts=4 sw=4 expandtab:

weather=${ascom_server}/observingConditions/0

function weather_drivername() {
    GET ${weather}/Name
}

function weather_driverversion() {
    GET ${weather}/DriverVersion
}

function weather_driverinfo() {
    GET ${weather}/DriverInfo
}

function weather_humidity() {
    GET ${weather}/humidity
}

function weather_winddirection() {
    GET ${weather}/windDirection
}

function weather_windspeed_mps() {
    GET ${weather}/windSpeed
}

function weather_windspeed_kmh() {
    local mps=$(ascom_weather_windspeed_mps)

    awk -v mps=${mps} 'BEGIN { print mps * 3.6; exit 0 }'
}

function weather_temperature() {
    GET ${weather}/temperature
}

function weather_rainrate() {
    GET ${weather}/rainRate
}

function weather_dewpoint() {
    GET ${weather}/dewPoint
}

function weather_pressure() {
    GET ${weather}/pressure
}

function weather_cloudcover() {
    GET ${weather}/cloudcover
}
