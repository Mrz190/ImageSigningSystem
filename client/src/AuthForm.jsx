import React, { useState } from "react";
import config from './config';
import CryptoJS from 'crypto-js';
import { useNavigate } from "react-router-dom";

const calculateDigest = (username, password, realm, nonce, uri, method, qop, nc, cnonce) => {
    const HA1 = CryptoJS.MD5(username + ":" + realm + ":" + password).toString(CryptoJS.enc.Hex);
    const HA2 = CryptoJS.MD5(method + ":" + uri).toString(CryptoJS.enc.Hex);
    const response = CryptoJS.MD5(HA1 + ":" + nonce + ":" + nc + ":" + cnonce + ":" + qop + ":" + HA2).toString(CryptoJS.enc.Hex);
    return response;
};

const AuthForm = () => {
    const [loginData, setLoginData] = useState({ userName: "", password: "" });
    const [registrationData, setRegistrationData] = useState({ username: "", password: "", email: "" });
    const [registrationSuccess, setRegistrationSuccess] = useState(false);
    const navigate = useNavigate();

    const handleLoginChange = (e) => {
        setLoginData({ ...loginData, [e.target.name]: e.target.value });
    };

    const handleRegistrationChange = (e) => {
        setRegistrationData({ ...registrationData, [e.target.name]: e.target.value });
    };

    const handleLoginSubmit = async (e) => {
        e.preventDefault();

        try {
            const nonceResponse = await fetch(`${config.apiBaseUrl}/Account/LoginNonce`, {
                method: "GET",
            });

            if (!nonceResponse.ok) {
                throw new Error("Не удалось получить nonce");
            }
            const nonceData = await nonceResponse.json();

            const username = loginData.userName;
            const password = loginData.password;
            const realm = nonceData.realm;
            const uri = "/Account/Login";
            const method = "POST";
            const qop = "auth";
            const nc = "00000001";
            const cnonce = CryptoJS.lib.WordArray.random(8).toString(CryptoJS.enc.Hex);

            const digest = calculateDigest(username, password, realm, nonceData.nonce, uri, method, qop, nc, cnonce);

            const response = await fetch(`${config.apiBaseUrl}/Account/Login`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "Authorization": `Digest username="${username}", realm="${realm}", nonce="${nonceData.nonce}", uri="${uri}", qop="${qop}", nc=${nc}, cnonce="${cnonce}", response="${digest}", opaque="${nonceData.opaque}"`,
                },
                body: JSON.stringify(loginData),
            });

            if (response.ok) {
                const data = await response.json();

                const userPasswordHash = CryptoJS.MD5(`${loginData.userName}:${nonceData.realm}:${loginData.password}`).toString(CryptoJS.enc.Hex);
                sessionStorage.setItem('userPasswordHash', userPasswordHash); // Используем sessionStorage

                if (data.token) {
                    sessionStorage.setItem('token', data.token); // Используем sessionStorage
                }
                if (data.role) {
                    sessionStorage.setItem('role', data.role); // Используем sessionStorage
                }
                sessionStorage.setItem('realm', realm); // Используем sessionStorage
                sessionStorage.setItem('username', username); // Используем sessionStorage
                location.reload();
                navigate("/home");
            } else {
                alert("Login failed. Please check your credentials.");
            }
        } catch (error) {
            console.error("Error during login:", error);
        }
    };

    const handleRegistrationSubmit = async (e) => {
        e.preventDefault();
        if (document.querySelector('[name="username"]').value.length < 3) {
            alert("The name must be 3 characters or more.");
            return;
        }
        try {
            const response = await fetch(`${config.apiBaseUrl}/Account/Registration`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify(registrationData),
            });

            if (response.ok) {
                const data = await response.json();
                setRegistrationSuccess(true);
                setRegistrationData({ username: "", password: "", email: "" });
            } else {
                alert("Registration failed. Please try again.");
            }
        } catch (error) {
            console.error("Error during registration:", error);
        }
    };

    return (
        <div className="main">
            <input type="checkbox" id="chk" aria-hidden="true" />
            <div className="signup">
                <form onSubmit={handleRegistrationSubmit}>
                    <label htmlFor="chk" aria-hidden="true">Sign up</label>
                    <input
                        className="reglog-input reg-input"
                        type="text"
                        name="username"
                        placeholder="Username"
                        required
                        autoComplete="off"
                        value={registrationData.username}
                        onChange={handleRegistrationChange}
                    />
                    <input
                        className="reglog-input reg-input"
                        type="email"
                        name="email"
                        placeholder="Email"
                        required
                        autoComplete="off"
                        value={registrationData.email}
                        onChange={handleRegistrationChange}
                    />
                    <input
                        className="reglog-input reg-input"
                        type="password"
                        name="password"
                        placeholder="Password"
                        required
                        autoComplete="off"
                        value={registrationData.password}
                        onChange={handleRegistrationChange}
                    />
                    <button className="send-btn" type="submit">Sign up</button>
                </form>

                {registrationSuccess && (
                    <div className="success-message">
                        <p>Registration successful! Please log in.</p>
                    </div>
                )}
            </div>

            <div className="login">
                <form onSubmit={handleLoginSubmit}>
                    <label htmlFor="chk" aria-hidden="true">Login</label>
                    <input
                        className="reglog-input log-input"
                        type="text"
                        name="userName"
                        placeholder="Username"
                        required
                        autoComplete="off"
                        value={loginData.userName}
                        onChange={handleLoginChange}
                    />
                    <input
                        className="reglog-input log-input"
                        type="password"
                        name="password"
                        placeholder="Password"
                        required
                        autoComplete="off"
                        value={loginData.password}
                        onChange={handleLoginChange}
                    />
                    <button className="send-btn" type="submit">Login</button>
                </form>
            </div>
        </div>
    );
};

export default AuthForm;
