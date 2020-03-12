#ifndef _SOCKET_PACKET_COLOUR_H_
#define _SOCKET_PACKET_COLOUR_H_

#include "SocketPacket.h"

// ���͵�������Ϣ
class SocketPacketColour : public SocketPacket
{
public:
	SocketPacketColour(const SOCKET_PACKET& type)
		:
		SocketPacket(type)
	{
		fillParams();
		zeroParams();
	}
	virtual void execute(){}
	virtual void fillParams()
	{
		pushArrayParam(mData, 8, "data");
	}
	// ��ɫRGB
	void setColour(char r, char g, char b);
protected:
	unsigned char mData[8];
};

#endif