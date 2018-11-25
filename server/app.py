import traceback
import shelve
import logging
import datetime
import hashlib
import struct
import base64
import hmac
from logging import handlers
from pprint import pformat

import requests
from flask import Flask, request, render_template, jsonify
from flask_httpauth import HTTPBasicAuth

import key

# disable flask and requests info logs
logging.getLogger('werkzeug').setLevel(logging.ERROR)
logging.getLogger("requests").setLevel(logging.ERROR)

app = Flask(__name__)
auth = HTTPBasicAuth()

logger = logging.getLogger()
logger.setLevel(level=logging.INFO)
file_handler = handlers.RotatingFileHandler(
    'logs/server.log', maxBytes=10 * 1024 * 1024, backupCount=1, encoding='utf-8')
formatter = logging.Formatter('%(asctime)s %(levelname)s: %(message)s')
file_handler.setFormatter(formatter)
logger.addHandler(file_handler)


@app.route('/api/logerror', methods=['POST'])
@auth.login_required
def log_error():
    with open('logs/errors.txt', 'a+', encoding='utf-8') as f:
        f.write(request.data.decode('utf-8') + '\n\n')
    return "OK", 200


@app.route('/api/showerrors', methods=['GET'])
@auth.login_required
def show_errors():
    with open('logs/errors.txt', 'r') as f:
        return f.read(), 200


@app.route('/api/showdb', methods=['GET'])
@auth.login_required
def show_db():
    with shelve.open('database/clients') as db:
        try:
            return render_template('clients.html', database=dict(db), pprint=pformat), 200
        except:
            logger.error(traceback.print_exc()), 500


@app.route('/api/getlicense/<int:subscription_time>', methods=['GET'])
@auth.login_required
def get_license(subscription_time):
    try:
        licenses = key.generate(subscription_time)
        return ",".join(licenses), 200
    except Exception:
        error = traceback.print_exc()
        logger.error(error)
        return error, 500


@app.route('/api/getlicensestatus', methods=['POST'])
@auth.login_required
def get_license_status():
    keys = request.data.decode("utf-8").split(",")
    response = {}
    with shelve.open("database/clients") as db:
        for item in keys:
            try:
                response[item] = db[item]
            except KeyError:
                response[item] = None

    return jsonify(response), 200


@app.route('/api/extendlicense', methods=['POST'])
@auth.login_required
def extend_license():
    key, subscription_time = request.data.decode('utf-8').split(',')
    try:
        subscription_time = int(subscription_time)
        with shelve.open("database/clients", writeback=True) as db:
            db[key]["subscription_time"] += subscription_time
        return "OK", 200
    except Exception:
        error = traceback.print_exc()
        logger.error(error)
        return error, 500


@app.route('/api/checklicense', methods=['POST'])
def check_license():
    success = False
    data = {key: value for key, value in request.form.items()}
    ip = request.environ.get('HTTP_X_REAL_IP', request.remote_addr)
    key = data['key']
    db = shelve.open("database/clients", writeback=True)
    try:
        if key in db:
            client = db[key]
            if client["subscription_time"] == 0:
                success = False
            else:
                hwid = data["hwid"]
                active_devices = client["devices"]
                if hwid not in active_devices:
                    city = get_city_from_ip(ip)
                    result = {
                        "connection_date": datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
                        "ip": (ip, city)
                    }
                    active_devices[hwid] = result
                    if client.get("initial_device", None) is None:
                        result["hwid"] = hwid
                        client["initial_device"] = result
                    logger.info("New device connected!: %s", active_devices[hwid])
                success = True
        else:
            logger.info('WRONG KEY: %s, %s\n', data, ip)
    finally:
        db.close()

    return jsonify({'success_3248237582': success}), 200


@app.route('/api/gdevid', methods=['POST'])
def generate_device_id():
    steam_id, key, hwid = request.data.decode('utf-8')
    ip = request.environ.get('HTTP_X_REAL_IP', request.remote_addr)
    success, error = validate_license(key, hwid, ip)
    if not success:
        return error, 402
    hexed_steam_id = hashlib.sha1(steam_id.encode('ascii')).hexdigest()
    return jsonify({'result_0x23432': 'android:' + '-'.join([hexed_steam_id[:8],
                                                     hexed_steam_id[8:12],
                                                     hexed_steam_id[12:16],
                                                      hexed_steam_id[16:20],
                                                     hexed_steam_id[20:32]])
            }), 200


@app.route('/api/gguardcode', methods=['POST'])
def generate_guard_code():
    ip = request.environ.get('HTTP_X_REAL_IP', request.remote_addr)
    shared_secret, timestamp, key, hwid = request.data.decode('utf-8').split(',')
    success, error = validate_license(key, hwid, ip)
    if not success:
        return error, 402
    timestamp = int(timestamp)
    time_buffer = struct.pack('>Q', timestamp // 30)  # pack as Big endian, uint64
    time_hmac = hmac.new(base64.b64decode(shared_secret), time_buffer, digestmod=hashlib.sha1).digest()
    begin = ord(time_hmac[19:20]) & 0xf
    full_code = struct.unpack('>I', time_hmac[begin:begin + 4])[0] & 0x7fffffff  # unpack as Big endian uint32
    chars = '23456789BCDFGHJKMNPQRTVWXY'
    code = ''

    for _ in range(5):
        full_code, i = divmod(full_code, len(chars))
        code += chars[i]

    return jsonify({'result_0x23432': code}), 200


@app.route('/api/gconfhash', methods=['POST'])
def generate_confirmation_hash():
    identity_secret, tag, timestamp, key, hwid = request.data.decode('utf-8').split(',')
    ip = request.environ.get('HTTP_X_REAL_IP', request.remote_addr)
    success, error = validate_license(key, hwid, ip)
    if not success:
        return error, 402
    timestamp = int(timestamp)
    buffer = struct.pack('>Q', timestamp) + tag.encode('ascii')
    key = base64.b64encode(hmac.new(base64.b64decode(identity_secret), buffer, digestmod=hashlib.sha1).digest())

    return jsonify({'result_0x23432': key}), 200


def get_city_from_ip(ip_address):
    try:
        resp = requests.get('http://ip-api.com/json/%s' % ip_address).json()
    except (requests.exceptions.ProxyError, request.exceptions.ConnectionError):
        return 'Unknown'
    return resp['city']


def update_database(data, db, key):
    db[key] = data
    logger.info('VALID KEY. Added to the database: %s\n', data)


def validate_license(key, ip, hwid):
    with shelve.open("database/clients", writeback=True):
        db = shelve.open("database/clients", writeback=True)
        try:
            client = db[key]
        except KeyError:
            return (False, "key was not found")
    if client["subscription_time"] == 0:
        return (False, "license expired")
    active_devices = client["devices"]
    if hwid not in active_devices:
        city = get_city_from_ip(ip)
        result = {
            "connection_date": datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
            "ip": (ip, city)
        }
        active_devices[hwid] = result

    return (True, None)


@auth.verify_password
def verify_password(username, password):
    if username == "steambiz777" and password == "XgnLJjQ0X5cG":
        return True

    return False

if __name__ == '__main__':
    app.run(debug=True)
    app.auto_reload = True
