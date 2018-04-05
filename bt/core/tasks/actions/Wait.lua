local Wait = bt.Class("Wait",bt.ActionTask)
bt.Wait = Wait

CompactStatus = {
    Failure = 0,
    Success = 1,
}

function Wait:ctor()
    bt.ActionTask.ctor(self)
    self.name = "Wait"
    self.waitTime = 1
    self.finishStatus = CompactStatus.Success
end

function Wait:init(jsonData)
    self:debug(self:getElapsedTime())
    if self:getElapsedTime() >= self.waitTime then
        if self.finishStatus == CompactStatus.Success then
            self:endAction(true)
        else
            self:endAction(false)
        end
    end
end