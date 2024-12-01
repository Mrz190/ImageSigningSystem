import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import CryptoJS from 'crypto-js';
import config from './config';

const Users = () => {
    const [users, setUsers] = useState({ users: [], admins: [], support: [] });
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const [activeTab, setActiveTab] = useState('users');
    const navigate = useNavigate();

    useEffect(() => {
        fetchUsers();
    }, []);

    const fetchUsers = async () => {
        setLoading(true);
        setError(null);
        try {
            const role = localStorage.getItem("role");
            const userPasswordHash = localStorage.getItem("userPasswordHash");
            const local_realm = localStorage.getItem("realm");
            const local_username = localStorage.getItem("username");

            if (!role || !userPasswordHash || !local_username) {
                throw new Error("User is not authenticated or role is missing");
            }

            const local_HA1 = userPasswordHash;
            const qop = "auth";
            const nc = "00000001";
            const cnonce = CryptoJS.lib.WordArray.random(4).toString(CryptoJS.enc.Hex);
            const uri = "/Admin/get-users";

            const nonceResponse = await fetch(`${config.apiBaseUrl}/Account/LoginNonce`, {
                method: "GET",
            });

            if (!nonceResponse.ok) {
                throw new Error("Failed to fetch nonce");
            }

            const nonceData = await nonceResponse.json();
            const nonce = nonceData.nonce;

            const digest = calculateDigest(local_HA1, nonce, uri, "GET", qop, nc, cnonce);

            const response = await Promise.all([
                fetch(`${config.apiBaseUrl}/Admin/get-support`, {
                    method: "GET",
                    headers: {
                        Authorization: `Digest username="${local_username}", realm="${local_realm}", nonce="${nonce}", uri="${uri}", algorithm="MD5", qop=${qop}, nc=${nc}, cnonce="${cnonce}", response="${digest}"`,
                    },
                }),
                fetch(`${config.apiBaseUrl}/Admin/get-users`, {
                    method: "GET",
                    headers: {
                        Authorization: `Digest username="${local_username}", realm="${local_realm}", nonce="${nonce}", uri="${uri}", algorithm="MD5", qop=${qop}, nc=${nc}, cnonce="${cnonce}", response="${digest}"`,
                    },
                }),
                fetch(`${config.apiBaseUrl}/Admin/get-admins`, {
                    method: "GET",
                    headers: {
                        Authorization: `Digest username="${local_username}", realm="${local_realm}", nonce="${nonce}", uri="${uri}", algorithm="MD5", qop=${qop}, nc=${nc}, cnonce="${cnonce}", response="${digest}"`,
                    },
                }),
            ]);

            if (!response[0].ok || !response[1].ok || !response[2].ok) {
                throw new Error('Failed to fetch users');
            }

            const supportUsers = await response[0].json();
            const regularUsers = await response[1].json();
            const adminUsers = await response[2].json();

            setUsers({
                support: supportUsers,
                users: regularUsers,
                admins: adminUsers,
            });
        } catch (err) {
            setError('Failed to fetch users');
        } finally {
            setLoading(false);
        }
    };

    const calculateDigest = (_local_HA1, nonce, uri, method, qop, nc, cnonce) => {
        const HA1 = _local_HA1;
        const HA2 = CryptoJS.MD5(method + ":" + uri).toString(CryptoJS.enc.Hex);
        const response = CryptoJS.MD5(HA1 + ":" + nonce + ":" + nc + ":" + cnonce + ":" + qop + ":" + HA2).toString(CryptoJS.enc.Hex);
        return response;
    };

    const handleRoleChange = async (userId, newRole) => {
        try {
            const roleMap = {
                'User': 3,
                'Admin': 1,
                'Support': 2,
            };

            const roleType = roleMap[newRole] || 3;

            const userPasswordHash = localStorage.getItem("userPasswordHash");
            const local_realm = localStorage.getItem("realm");
            const local_username = localStorage.getItem("username");

            if (!userPasswordHash || !local_username) {
                throw new Error("Missing authentication details");
            }

            const local_HA1 = userPasswordHash;
            const qop = "auth";
            const nc = "00000001";
            const cnonce = CryptoJS.lib.WordArray.random(4).toString(CryptoJS.enc.Hex);
            const uri = `/Admin/change-role/${userId}?roleType=${roleType}`;

            const nonceResponse = await fetch(`${config.apiBaseUrl}/Account/LoginNonce`, {
                method: "GET",
            });

            if (!nonceResponse.ok) {
                throw new Error("Failed to fetch nonce");
            }

            const nonceData = await nonceResponse.json();
            const nonce = nonceData.nonce;

            const digest = calculateDigest(local_HA1, nonce, uri, "POST", qop, nc, cnonce);

            const response = await fetch(`${config.apiBaseUrl}/Admin/change-role/${userId}?roleType=${roleType}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    Authorization: `Digest username="${local_username}", realm="${local_realm}", nonce="${nonce}", uri="${uri}", algorithm="MD5", qop=${qop}, nc=${nc}, cnonce="${cnonce}", response="${digest}"`,
                },
            });

            if (!response.ok) {
                throw new Error('Failed to change role');
            }

            alert("User role updated.");

            fetchUsers();
        } catch (error) {
            alert(`Error: ${error.message}`);
        }
    };

    const handleTabChange = (tab) => {
        setActiveTab(tab);
    };

    const handleRedirectToHomePage = () => {
        navigate("/home", { replace: true });
    };

    const renderUserTable = (usersList) => {
        return (
            <div className="table-wrapper">
                <table className="images-table">
                    <thead>
                        <tr>
                            <th>ID</th>
                            <th>Username</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        {usersList.length > 0 ? (
                            usersList.map((user) => (
                                <tr key={user.id}>
                                    <td>{user.id}</td>
                                    <td>{user.userName}</td>
                                    <td className="td-actions">
                                        <button className="change-role-btn"
                                            onClick={() => handleRoleChange(user.id, 'User')}
                                            disabled={user.role === 'User'}
                                        >
                                            Set as User
                                        </button>
                                        <button className="change-role-btn"
                                            onClick={() => handleRoleChange(user.id, 'Admin')}
                                            disabled={user.role === 'Admin'}
                                        >
                                            Set as Admin
                                        </button>
                                        <button className="change-role-btn"
                                            onClick={() => handleRoleChange(user.id, 'Support')}
                                            disabled={user.role === 'Support'}
                                        >
                                            Set as Support
                                        </button>
                                    </td>
                                </tr>
                            ))
                        ) : (
                            <tr>
                                <td colSpan="4">No users found</td>
                            </tr>
                        )}
                    </tbody>
                </table>
            </div>
        );
    };


    return (
        <div className="global-container">
            <button className="redirect-btn back-btn" onClick={() => handleRedirectToHomePage()}>Home</button>
            <div className="tabs">
                <button
                    className={`get-users-btn ${activeTab === 'users' ? 'active-tab' : ''}`}
                    onClick={() => handleTabChange('users')}
                >
                    Users
                </button>
                <button
                    className={`get-users-btn ${activeTab === 'admins' ? 'active-tab' : ''}`}
                    onClick={() => handleTabChange('admins')}
                >
                    Admins
                </button>
                <button
                    className={`get-users-btn ${activeTab === 'support' ? 'active-tab' : ''}`}
                    onClick={() => handleTabChange('support')}
                >
                    Support
                </button>
            </div>

            <div className="api-buttons">
                <button className="reload-btn" onClick={() => fetchUsers()} disabled={loading}></button>
            </div>

            {error && <div className="error">{error}</div>}

            <div className="user-table-container">
                {loading ? (
                    <div className="loading">Loading users...</div>
                ) : (
                    renderUserTable(users[activeTab])
                )}
            </div>
        </div>
    );
};

export default Users;
